using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Grading;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Grading;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;
using STEM.Infrastructure.Repositories;
using STEM.Infrastructure.Services;

namespace STEM.Application.Tests;

// Gate tests required by the "Virtual Lab submission + teacher grading flow"
// audit task (Phase 19: snapshot immutability, Phase 20: roster completeness).
// Reuses the same fakes/harness pattern as VirtualLabSubmissionRoundTripTests.cs.
public sealed class VirtualLabSubmissionFlowGateTests
{
    private sealed class ThrowingRunnerResolver : ISimulationRunnerResolver
    {
        public ISimulationRunner Resolve(string mode) => throw new InvalidOperationException(
            "Not expected to be called on the Submit/SaveDiagram path in this test.");
    }

    // Immutability test provides a real SessionId (unlike VirtualLabSubmissionRoundTripTests.cs),
    // so SubmitVirtualLabAsync's compile-check tier DOES run — stub it as a
    // successful compile instead of throwing; compile correctness isn't what
    // this test is proving.
    private sealed class StubCompileService : ISimulationCompileService
    {
        public Task<CompileSimulationResponse> CompileAsync(CompileSimulationRequest request, int currentUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CompileSimulationResponse { Success = true });

        public Task<CompileJobResponse?> GetJobAsync(string jobId, int currentUserId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Not expected to be called.");
    }

    private sealed class ThrowingPrecompileTrigger : IPrecompileTriggerService
    {
        public void TriggerBackgroundCompile(string sourceCode, string board, string framework, Guid? buildCacheScopeId) =>
            throw new InvalidOperationException("Not expected to be called (Start-only path).");
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

    private const string DiagramA = """{ "board": "esp32_devkit_v1", "parts": [{ "id": "ledA", "type": "wokwi-led" }], "connections": [] }""";
    private const string CodeA = "void setup() {} void loop() { /* version A */ }";

    private const string DiagramB = """{ "board": "esp32_devkit_v1", "parts": [{ "id": "ledB", "type": "wokwi-led" }], "connections": [] }""";
    private const string CodeB = "void setup() {} void loop() { /* version B — edited AFTER submit */ }";

    // PHASE 19 GATE: submit V1 (code A / diagram A), then keep editing the SAME
    // VirtualLabProject to V2 (code B / diagram B) exactly like autosave does
    // (SaveDiagramAsync). Reloading the Submission from a fresh DbContext must
    // still show A — a Submission is a point-in-time snapshot, not a live view
    // of the project.
    [Fact]
    public async Task Submission_StaysAtSubmitTimeSnapshot_AfterProjectIsEditedAgain()
    {
        var dbOptions = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"submission-immutability-{Guid.NewGuid():N}")
            .Options;

        var projectId = Guid.NewGuid();
        int assignmentId;
        int submissionId;
        const int studentId = 9001;

        await using (var writeContext = new StemDbContext(dbOptions))
        {
            var testClass = new Class
            {
                ClassCode = "TEST-IMMUTABILITY",
                SchoolId = 1,
                CourseId = 1,
                TeacherId = 1,
                GradeLevelId = 1,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
            };
            writeContext.Classes.Add(testClass);
            await writeContext.SaveChangesAsync();

            var assignment = new Assignment
            {
                ClassId = testClass.Id,
                Title = "Immutability Gate Assignment",
                MaxScore = 100,
                AllowResubmit = true,
                ResubmitLimit = 5,
            };
            writeContext.Assignments.Add(assignment);
            await writeContext.SaveChangesAsync();
            assignmentId = assignment.Id;

            var service = new VirtualLabRuntimeService(
                writeContext,
                new VirtualLabDiagramService(),
                new ThrowingRunnerResolver(),
                new StubCompileService(),
                new ConfigurationManager(),
                new RunningSimulationRegistry(),
                new ThrowingPrecompileTrigger(),
                new SystemDateTimeProvider(),
                new NoOpNotificationRepository());

            // Student autosaves version A, then submits it.
            await service.SaveDiagramAsync(
                projectId.ToString("N"),
                new SaveDiagramRequest { DiagramJson = DiagramA, SourceCode = CodeA, LabId = null },
                studentId,
                CancellationToken.None);

            var submitResponse = await service.SubmitVirtualLabAsync(new VirtualLabSubmissionRequest
            {
                AssignmentId = assignmentId,
                SessionId = projectId.ToString("N"),
                StudentId = studentId,
                DiagramJson = DiagramA,
                SourceCode = CodeA,
            }, currentUserId: studentId, CancellationToken.None);
            submissionId = submitResponse.SubmissionId;

            // Student keeps tinkering AFTER submitting — same project, version B.
            await service.SaveDiagramAsync(
                projectId.ToString("N"),
                new SaveDiagramRequest { DiagramJson = DiagramB, SourceCode = CodeB, LabId = null },
                studentId,
                CancellationToken.None);

            var mutatedProject = await writeContext.VirtualLabProjects.AsNoTracking().FirstAsync(p => p.Id == projectId);
            Assert.Equal(CodeB, mutatedProject.CodeContent); // sanity: the mutation actually happened.
        }

        await using var readContext = new StemDbContext(dbOptions);
        var savedSubmission = await readContext.Submissions.AsNoTracking().FirstAsync(s => s.Id == submissionId);

        Assert.Contains(CodeA, savedSubmission.ContentJson);
        Assert.DoesNotContain(CodeB, savedSubmission.ContentJson);
        Assert.Contains("ledA", savedSubmission.ContentJson);
        Assert.DoesNotContain("ledB", savedSubmission.ContentJson);
    }

    // PHASE 20 GATE: a class of 3 enrolled students where only 1 has submitted.
    // GetSubmissionsHandler, filtered by assignmentId, must report all 3 —
    // not just the 1 row that actually exists in Submissions.
    [Fact]
    public async Task GetSubmissions_FilteredByAssignment_ReportsEnrolledStudentsWithNoSubmissionToo()
    {
        var dbOptions = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"submission-roster-{Guid.NewGuid():N}")
            .Options;

        await using var context = new StemDbContext(dbOptions);

        var teacherRole = new Role { Name = RoleNames.Teacher };
        var studentRole = new Role { Name = RoleNames.Student };
        context.Roles.AddRange(teacherRole, studentRole);
        await context.SaveChangesAsync();

        var teacher = new User { FullName = "Teacher T", Email = "teacher@test.local", RoleId = teacherRole.Id, Role = teacherRole };
        var studentA = new User { FullName = "Student A", Email = "a@test.local", RoleId = studentRole.Id, Role = studentRole };
        var studentB = new User { FullName = "Student B", Email = "b@test.local", RoleId = studentRole.Id, Role = studentRole };
        var studentC = new User { FullName = "Student C", Email = "c@test.local", RoleId = studentRole.Id, Role = studentRole };
        context.Users.AddRange(teacher, studentA, studentB, studentC);
        await context.SaveChangesAsync();

        // SubmissionRepository.BuildDetailsQuery() chains Include(Assignment).
        // ThenInclude(Class).ThenInclude(Course/School) — EF InMemory silently
        // drops the ROOT Submission row when a required-FK navigation further
        // down the chain (Class.Course / Class.School here) has no matching
        // row, same quirk already documented in
        // VirtualLabSubmissionRoundTripTests.cs for Assignment.Class. Real
        // Course/School rows are required here for that reason, not because
        // this test cares about their content.
        var school = new School { Name = "TEST-ROSTER-SCHOOL" };
        var course = new Course { Title = "TEST-ROSTER-COURSE" };
        context.Schools.Add(school);
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var testClass = new Class
        {
            ClassCode = "TEST-ROSTER",
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
            new Enrollment { ClassId = testClass.Id, StudentId = studentB.Id, EnrolledAt = DateTime.UtcNow },
            new Enrollment { ClassId = testClass.Id, StudentId = studentC.Id, EnrolledAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var assignment = new Assignment
        {
            ClassId = testClass.Id,
            Title = "Roster Completeness Gate Assignment",
            MaxScore = 100,
        };
        context.Assignments.Add(assignment);
        await context.SaveChangesAsync();

        // Only Student A submits.
        context.Submissions.Add(new Submission
        {
            AssignmentId = assignment.Id,
            StudentId = studentA.Id,
            Status = SubmissionStatuses.Submitted,
            SubmittedAt = DateTime.UtcNow,
            ContentJson = "{}",
        });
        await context.SaveChangesAsync();

        var handler = new GetSubmissionsHandler(
            new SubmissionRepository(context),
            new UserRepository(context),
            new AssignmentRepository(context),
            new EnrollmentRepository(context),
            new ClassRepository(context));

        var response = await handler.Handle(
            new GetSubmissionsRequest { AssignmentId = assignment.Id, PageSize = 100 },
            currentUserId: teacher.Id,
            CancellationToken.None);

        Assert.Equal(3, response.Items.Count);

        var byStudent = response.Items.ToDictionary(item => item.StudentId!.Value);

        Assert.True(byStudent.ContainsKey(studentA.Id));
        Assert.NotEqual("not_submitted", byStudent[studentA.Id].Status);
        Assert.True(byStudent[studentA.Id].Id > 0);

        Assert.True(byStudent.ContainsKey(studentB.Id));
        Assert.Equal("not_submitted", byStudent[studentB.Id].Status);
        Assert.Equal(0, byStudent[studentB.Id].Id);
        Assert.Equal("Student B", byStudent[studentB.Id].StudentName);

        Assert.True(byStudent.ContainsKey(studentC.Id));
        Assert.Equal("not_submitted", byStudent[studentC.Id].Status);
        Assert.Equal(0, byStudent[studentC.Id].Id);
    }
}
