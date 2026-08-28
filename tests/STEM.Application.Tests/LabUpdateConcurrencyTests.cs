using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using STEM.Application.Dtos.Labs;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Users;
using STEM.Infrastructure.Data;
using STEM.Infrastructure.Services;

namespace STEM.Application.Tests;

// FIX ONLY LAB UPDATE 500 ERROR task (2026-08-27). Root cause: SyncClassAssignments
// added a brand-new LabClassAssignment via the already-tracked parent's
// navigation collection ("lab.ClassAssignments.Add(...)") instead of the
// DbSet directly. Because the new row's Guid Id is client-assigned (not
// database-generated) BEFORE the Add call, EF Core's change detection for a
// navigation-collection add reads the pre-set key as "this must already
// exist" and marks the entry State=Modified instead of Added — confirmed
// live via a ChangeTracker dump showing exactly that on the real bug
// reproduction. SaveChangesAsync then emits an UPDATE ... WHERE Id = @id for
// a row that was never inserted -> 0 rows affected ->
// DbUpdateConcurrencyException, wrapped by the retry loop into
// "Lab was modified by another user" after 3 identical failures -> HTTP 500.
// The fix (STEM.Infrastructure/Services/LabService.cs, SyncClassAssignments):
// add the new LabClassAssignment via _context.LabClassAssignments.Add(...)
// instead, which always marks a fresh entity Added regardless of its key
// value. These tests pin exactly the update paths that used to throw.
//
// Uses EF Core's InMemory provider (same convention as
// VirtualLabSubmissionRoundTripTests.cs) — NOT a mock of LabService, this is
// the real production LabService.UpdateLabAsync/SyncClassAssignments running
// against a real DbContext SaveChangesAsync. InMemory reproduces the actual
// bug faithfully (its own SaveChanges also throws DbUpdateConcurrencyException
// when asked to "update" a key that isn't in its store — exactly the wrong
// state this bug produced), which is what these tests assert against.
//
// Assertions read the persisted LabClassAssignment rows directly off the
// DbContext rather than through UpdateLabAsync's mapped LabResponse.ClassIds:
// BuildLabQuery's multi-branch Include chain (ClassAssignments -> Class ->
// Course, and separately -> Enrollments, off the same collection) hits a
// known EF Core InMemory-provider limitation combined with AsNoTracking()
// that drops the nested Class reference — confirmed by hand (a plain
// `context.LabClassAssignments.Include(a => a.Class)` query resolves Class
// correctly; it's specifically BuildLabQuery's branching Include shape that
// doesn't, only under InMemory). That's a test-double limitation, not a
// production bug: the real Npgsql-backed flow was independently verified
// live in-browser to return classIds correctly (see FINAL REPORT).
public sealed class LabUpdateConcurrencyTests
{
    private sealed class NoOpPrecompileTrigger : IPrecompileTriggerService
    {
        public void TriggerBackgroundCompile(string sourceCode, string board, string framework, Guid? buildCacheScopeId)
        {
        }
    }

    private static async Task<(StemDbContext Context, User Teacher, Class TestClass, Lab Lab)> SeedAsync(
        DbContextOptions<StemDbContext> options)
    {
        var context = new StemDbContext(options);

        var role = new Role { Name = RoleNames.Teacher };
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var teacher = new User
        {
            Email = $"teacher-{Guid.NewGuid():N}@example.com",
            FullName = "Test Teacher",
            RoleId = role.Id,
            IsActive = true,
        };
        context.Users.Add(teacher);
        await context.SaveChangesAsync();

        var testClass = new Class
        {
            ClassCode = $"TEST-{Guid.NewGuid():N}"[..12],
            SchoolId = 1,
            CourseId = 1,
            TeacherId = teacher.Id,
            GradeLevelId = 1,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(6),
        };
        context.Classes.Add(testClass);
        await context.SaveChangesAsync();

        var lab = new Lab
        {
            Id = Guid.NewGuid(),
            Title = "Lab Update Concurrency Test",
            Description = "seed",
            Category = LabCategories.Robotics,
            // custom_sandbox, not the wokwi_iframe default: SimulationMode ==
            // wokwi_iframe requires a WokwiProjectId/Url (PreparePayload,
            // LabService.cs:868-873) — unrelated to this bug, and the real
            // Lab this bug was found on ("Robot giao hàng mini") is
            // custom_sandbox too.
            SimulationMode = LabSimulationModes.CustomSandbox,
            BoardType = LabBoardTypes.ArduinoUno,
            Status = LabStatuses.Draft,
            CreatedById = teacher.Id,
        };
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        return (context, teacher, testClass, lab);
    }

    private static UpdateLabRequest BaseRequest(Lab lab, IReadOnlyCollection<int> classIds, int? scheduleId) => new()
    {
        Title = lab.Title,
        Description = lab.Description,
        Category = lab.Category,
        SimulationMode = lab.SimulationMode,
        BoardType = lab.BoardType,
        Status = LabStatuses.Draft,
        ClassIds = classIds,
        ScheduleId = scheduleId,
    };

    [Fact]
    public async Task UpdateLabAsync_AssignsNewClass_WithoutScheduleId_DoesNotThrow()
    {
        var options = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"lab-update-{Guid.NewGuid():N}")
            .Options;
        var (context, teacher, testClass, lab) = await SeedAsync(options);
        await using var _ = context;
        var service = new LabService(context, new HttpClient(), NullLogger<LabService>.Instance, new NoOpPrecompileTrigger());

        // Before the fix, this line threw InvalidOperationException("Lab was
        // modified by another user...") wrapping a DbUpdateConcurrencyException,
        // on every attempt (not a transient race) — see file header.
        await service.UpdateLabAsync(
            lab.Id,
            BaseRequest(lab, new[] { testClass.Id }, scheduleId: null),
            teacher.Id,
            CancellationToken.None);

        var saved = await context.LabClassAssignments.AsNoTracking().SingleAsync(a => a.LabId == lab.Id);
        Assert.Equal(testClass.Id, saved.ClassId);
        Assert.Null(saved.ScheduleId);
    }

    [Fact]
    public async Task UpdateLabAsync_AssignsNewClass_WithScheduleId_DoesNotThrow()
    {
        var options = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"lab-update-{Guid.NewGuid():N}")
            .Options;
        var (context, teacher, testClass, lab) = await SeedAsync(options);
        await using var _ = context;
        var service = new LabService(context, new HttpClient(), NullLogger<LabService>.Instance, new NoOpPrecompileTrigger());

        // scheduleId doesn't need to reference a real Schedule row for THIS
        // bug (nullable FK, SetNull on delete) — the point of this test is
        // the LabClassAssignment insert path, not schedule-id validation
        // (there is none — see FINAL REPORT's KNOWN GAPS).
        await service.UpdateLabAsync(
            lab.Id,
            BaseRequest(lab, new[] { testClass.Id }, scheduleId: 999),
            teacher.Id,
            CancellationToken.None);

        var saved = await context.LabClassAssignments.AsNoTracking().SingleAsync(a => a.LabId == lab.Id);
        Assert.Equal(testClass.Id, saved.ClassId);
        Assert.Equal(999, saved.ScheduleId);
    }

    [Fact]
    public async Task UpdateLabAsync_CalledTwiceWithSameClass_IsIdempotent_DoesNotThrow()
    {
        var options = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"lab-update-{Guid.NewGuid():N}")
            .Options;
        var (context, teacher, testClass, lab) = await SeedAsync(options);
        await using var _ = context;
        var service = new LabService(context, new HttpClient(), NullLogger<LabService>.Instance, new NoOpPrecompileTrigger());

        await service.UpdateLabAsync(
            lab.Id,
            BaseRequest(lab, new[] { testClass.Id }, scheduleId: null),
            teacher.Id,
            CancellationToken.None);

        // Re-save with the exact same classIds — must not create a duplicate
        // LabClassAssignment row (unique index on LabId+ClassId) or throw,
        // matching STEP 8's "edit without changes" / "re-save same class".
        await service.UpdateLabAsync(
            lab.Id,
            BaseRequest(lab, new[] { testClass.Id }, scheduleId: null),
            teacher.Id,
            CancellationToken.None);

        var savedRows = await context.LabClassAssignments.AsNoTracking().Where(a => a.LabId == lab.Id).ToListAsync();
        Assert.Single(savedRows);
        Assert.Equal(testClass.Id, savedRows[0].ClassId);
    }

    [Fact]
    public async Task UpdateLabAsync_WithNoClassAssigned_StillSucceeds()
    {
        var options = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"lab-update-{Guid.NewGuid():N}")
            .Options;
        var (context, teacher, _, lab) = await SeedAsync(options);
        await using var _ = context;
        var service = new LabService(context, new HttpClient(), NullLogger<LabService>.Instance, new NoOpPrecompileTrigger());

        await service.UpdateLabAsync(
            lab.Id,
            BaseRequest(lab, Array.Empty<int>(), scheduleId: null),
            teacher.Id,
            CancellationToken.None);

        var savedRows = await context.LabClassAssignments.AsNoTracking().Where(a => a.LabId == lab.Id).ToListAsync();
        Assert.Empty(savedRows);
    }
}
