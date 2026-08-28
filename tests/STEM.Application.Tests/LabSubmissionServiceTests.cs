using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Labs;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;
using STEM.Infrastructure.Services;

namespace STEM.Application.Tests;

// Lesson->Lab->Submission flow: Assignment phải là chi tiết ẩn (tự tạo khi
// cần), Teacher/Student không bao giờ thao tác trực tiếp với nó. Xem
// LabSubmissionService.cs.
public sealed class LabSubmissionServiceTests
{
    private sealed class ThrowingRunnerResolver : ISimulationRunnerResolver
    {
        public ISimulationRunner Resolve(string mode) => throw new InvalidOperationException("Not expected.");
    }

    private sealed class ThrowingCompileService : ISimulationCompileService
    {
        public Task<CompileSimulationResponse> CompileAsync(CompileSimulationRequest request, int currentUserId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Not expected — SessionId is null in these tests (no live project).");

        public Task<CompileJobResponse?> GetJobAsync(string jobId, int currentUserId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Not expected.");
    }

    private sealed class ThrowingPrecompileTrigger : IPrecompileTriggerService
    {
        public void TriggerBackgroundCompile(string sourceCode, string board, string framework, Guid? buildCacheScopeId) =>
            throw new InvalidOperationException("Not expected.");
    }

    private sealed class NoOpNotificationRepository : INotificationRepository
    {
        public Task<IEnumerable<Notification>> GetByUserIdAsync(string userId, int skip, int take, NotificationType? type = null, NotificationStatus? status = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<Notification>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<Notification>> FindAsync(System.Linq.Expressions.Expression<Func<Notification, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Notification entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRangeAsync(IEnumerable<Notification> entities, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Update(Notification entity) => throw new NotSupportedException();
        public void Delete(Notification entity) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Notification entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static async Task<(StemDbContext Context, School School, Course Course, User Teacher, User StudentA, User StudentB, Class Class, Lab Lab)> SeedAsync(
        DbContextOptions<StemDbContext> options)
    {
        var context = new StemDbContext(options);

        var teacherRole = new Role { Name = RoleNames.Teacher };
        var studentRole = new Role { Name = RoleNames.Student };
        context.Roles.AddRange(teacherRole, studentRole);
        await context.SaveChangesAsync();

        var school = new School { Name = "TEST-SCHOOL" };
        var course = new Course { Title = "TEST-COURSE" };
        context.Schools.Add(school);
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var teacher = new User { FullName = "Teacher T", Email = "teacher@lesson-lab.local", RoleId = teacherRole.Id, Role = teacherRole, SchoolId = school.Id };
        var studentA = new User { FullName = "Student A", Email = "a@lesson-lab.local", RoleId = studentRole.Id, Role = studentRole };
        var studentB = new User { FullName = "Student B", Email = "b@lesson-lab.local", RoleId = studentRole.Id, Role = studentRole };
        context.Users.AddRange(teacher, studentA, studentB);
        await context.SaveChangesAsync();

        var testClass = new Class
        {
            ClassCode = "TEST-LESSON-LAB",
            SchoolId = school.Id,
            CourseId = course.Id,
            GradeLevelId = 1,
            TeacherId = teacher.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(6),
        };
        context.Classes.Add(testClass);
        await context.SaveChangesAsync();

        context.Enrollments.AddRange(
            new Enrollment { ClassId = testClass.Id, StudentId = studentA.Id, EnrolledAt = DateTime.UtcNow },
            new Enrollment { ClassId = testClass.Id, StudentId = studentB.Id, EnrolledAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var lab = new Lab
        {
            Id = Guid.NewGuid(),
            Title = "Push Button LED",
            BoardType = LabBoardTypes.Esp32DevkitV1,
            CreatedById = teacher.Id,
            Status = LabStatuses.Published,
        };
        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        context.LabClassAssignments.Add(new LabClassAssignment
        {
            Id = Guid.NewGuid(),
            LabId = lab.Id,
            ClassId = testClass.Id,
        });
        await context.SaveChangesAsync();

        return (context, school, course, teacher, studentA, studentB, testClass, lab);
    }

    private static LabSubmissionService BuildService(StemDbContext context)
    {
        var runtimeService = new VirtualLabRuntimeService(
            context,
            new VirtualLabDiagramService(),
            new ThrowingRunnerResolver(),
            new ThrowingCompileService(),
            new ConfigurationManager(),
            new RunningSimulationRegistry(),
            new ThrowingPrecompileTrigger(),
            new SystemDateTimeProvider(),
            new NoOpNotificationRepository());

        return new LabSubmissionService(context, runtimeService);
    }

    [Fact]
    public async Task Submit_WithNoLinkedAssignment_AutoCreatesHiddenAssignmentAndSucceeds()
    {
        var options = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"lab-submission-hidden-{Guid.NewGuid():N}")
            .Options;

        var (context, _, _, _, studentA, _, _, lab) = await SeedAsync(options);
        await using var _1 = context;

        var service = BuildService(context);

        var response = await service.SubmitAsync(
            lab.Id,
            new SubmitLabRequest { DiagramJson = "{}", SourceCode = "void setup(){} void loop(){}" },
            studentA.Id,
            CancellationToken.None);

        Assert.True(response.SubmissionId > 0);

        var reloadedLab = await context.Labs.AsNoTracking().FirstAsync(item => item.Id == lab.Id);
        Assert.True(reloadedLab.LinkedAssignmentId.HasValue);

        var hiddenAssignment = await context.Assignments.AsNoTracking()
            .FirstAsync(item => item.Id == reloadedLab.LinkedAssignmentId!.Value);
        Assert.Equal(AssignmentTypes.PracticalSimulation, hiddenAssignment.AssignmentType);
        Assert.Contains(lab.Id.ToString("N"), hiddenAssignment.Description);
    }

    [Fact]
    public async Task Submit_Twice_ByDifferentStudents_ReusesSameHiddenAssignment_NoDuplicate()
    {
        var options = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"lab-submission-noduplicate-{Guid.NewGuid():N}")
            .Options;

        var (context, _, _, _, studentA, studentB, _, lab) = await SeedAsync(options);
        await using var _1 = context;

        var service = BuildService(context);

        await service.SubmitAsync(lab.Id, new SubmitLabRequest { DiagramJson = "{}", SourceCode = "// A" }, studentA.Id, CancellationToken.None);
        await service.SubmitAsync(lab.Id, new SubmitLabRequest { DiagramJson = "{}", SourceCode = "// B" }, studentB.Id, CancellationToken.None);

        var hiddenAssignmentCount = await context.Assignments.CountAsync(item => item.Description.Contains(lab.Id.ToString("N")));
        Assert.Equal(1, hiddenAssignmentCount);
    }

    [Fact]
    public async Task GetSubmissions_ReturnsFullRoster_SubmittedAndNotSubmitted()
    {
        var options = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"lab-submission-roster-{Guid.NewGuid():N}")
            .Options;

        var (context, _, _, teacher, studentA, studentB, testClass, lab) = await SeedAsync(options);
        await using var _1 = context;

        var service = BuildService(context);

        // Only Student A submits.
        await service.SubmitAsync(lab.Id, new SubmitLabRequest { DiagramJson = "{}", SourceCode = "// A" }, studentA.Id, CancellationToken.None);

        var response = await service.GetSubmissionsAsync(lab.Id, testClass.Id, teacher.Id, CancellationToken.None);

        Assert.Equal(2, response.TotalStudents);
        Assert.Equal(1, response.SubmittedCount);
        Assert.Equal(1, response.NotSubmittedCount);

        var rowA = Assert.Single(response.Students, item => item.StudentId == studentA.Id);
        Assert.Equal("submitted", rowA.Status);
        Assert.NotNull(rowA.SubmissionId);

        var rowB = Assert.Single(response.Students, item => item.StudentId == studentB.Id);
        Assert.Equal("not_started", rowB.Status);
        Assert.Null(rowB.SubmissionId);
    }

    [Fact]
    public async Task Submit_ByStudentNotEnrolledInAnyAssignedClass_Throws()
    {
        var options = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"lab-submission-unauthorized-{Guid.NewGuid():N}")
            .Options;

        var (context, _, _, _, _, _, _, lab) = await SeedAsync(options);
        await using var _1 = context;

        var outsiderRole = new Role { Name = RoleNames.Student };
        context.Roles.Add(outsiderRole);
        await context.SaveChangesAsync();
        var outsider = new User { FullName = "Outsider", Email = "outsider@lesson-lab.local", RoleId = outsiderRole.Id, Role = outsiderRole };
        context.Users.Add(outsider);
        await context.SaveChangesAsync();

        var service = BuildService(context);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SubmitAsync(lab.Id, new SubmitLabRequest { DiagramJson = "{}", SourceCode = "// x" }, outsider.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmissions_ByTeacherNotOwningClass_Throws()
    {
        var options = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"lab-submission-teacher-auth-{Guid.NewGuid():N}")
            .Options;

        var (context, school, _, _, _, _, testClass, lab) = await SeedAsync(options);
        await using var _1 = context;

        var otherTeacherRole = new Role { Name = RoleNames.Teacher };
        context.Roles.Add(otherTeacherRole);
        await context.SaveChangesAsync();
        var otherTeacher = new User { FullName = "Other Teacher", Email = "other@lesson-lab.local", RoleId = otherTeacherRole.Id, Role = otherTeacherRole, SchoolId = school.Id };
        context.Users.Add(otherTeacher);
        await context.SaveChangesAsync();

        var service = BuildService(context);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetSubmissionsAsync(lab.Id, testClass.Id, otherTeacher.Id, CancellationToken.None));
    }
}
