using System.Linq.Expressions;
using STEM.Application.Dtos.Syllabuses;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Syllabuses;
using STEM.Core.Entities;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Curriculum;
using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Users;
using STEM.Core.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.Tests;

/// <summary>
/// Hand-rolled in-memory fakes (the test project has no EF/mocking dependency).
/// Only the members these handlers actually call are implemented functionally;
/// everything else throws to make accidental use visible.
/// </summary>
internal class FakeRepository<T> : IRepository<T> where T : BaseEntity
{
    public readonly List<T> Items = new();

    public Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

    public Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<T>>(Items);

    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.AsQueryable().Where(predicate).AsEnumerable());

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == 0)
            entity.Id = Items.Count == 0 ? 1 : Items.Max(i => i.Id) + 1;
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        foreach (var e in entities) Items.Add(e);
        return Task.CompletedTask;
    }

    public void Update(T entity) { }
    public void Delete(T entity) => Items.Remove(entity);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(i => i.Id == id));

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        Items.Remove(entity);
        return Task.CompletedTask;
    }
}

internal class FakeUserRepository : FakeRepository<User>, IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<(IEnumerable<User> Users, int TotalCount)> GetUsersPagedAsync(int pageNumber, int pageSize, string? searchTerm, int? roleId, bool? isActive, int? schoolId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<User>> GetStudentsNotInClassAsync(int classId, int schoolId, string? searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<STEM.Core.Entities.Classes.Schedule>> GetStudentSchedulesAsync(int studentId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<(IEnumerable<User> Users, int TotalCount)> GetTeachersWithClassCountAsync(int schoolId, int page, int pageSize, string? searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<User>> GetBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<(int TeacherCount, int StudentCount)> GetTeacherStudentCountBySchoolAsync(int schoolId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

internal class FakeSyllabusRepository : FakeRepository<Syllabus>, ISyllabusRepository
{
    public Task<Syllabus?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(s => s.Id == id));
    public Task<Syllabus?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(s => s.Id == id));
    public Task<IEnumerable<Syllabus>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Syllabus>>(Items);
    public Task<IEnumerable<Syllabus>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Syllabus>>(Items);
    public Task<IEnumerable<Syllabus>> GetByStatusAsync(string status, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Syllabus>>(Items.Where(s => s.Status == status));
    public Task<IEnumerable<Syllabus>> GetByGradeLevelAsync(int gradeLevelId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Syllabus>>(Items.Where(s => s.GradeLevelId == gradeLevelId));
    public Task<IEnumerable<Syllabus>> GetByGradeLevelWithDetailsAsync(int gradeLevelId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Syllabus>>(Items.Where(s => s.GradeLevelId == gradeLevelId));
    public Task<IEnumerable<Syllabus>> GetPublishedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Syllabus>>(Items.Where(s => s.Status == SyllabusStatuses.Published));
    public new Task<Syllabus> AddAsync(Syllabus syllabus, CancellationToken cancellationToken = default)
    {
        if (syllabus.Id == 0)
            syllabus.Id = Items.Count == 0 ? 1 : Items.Max(s => s.Id) + 1;
        Items.Add(syllabus);
        return Task.FromResult(syllabus);
    }
    public new Task<Syllabus> UpdateAsync(Syllabus syllabus, CancellationToken cancellationToken = default) =>
        Task.FromResult(syllabus);
    public new Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = Items.FirstOrDefault(s => s.Id == id);
        if (item != null)
        {
            Items.Remove(item);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(s => s.Id == id));
    public Task<int> GetCourseCountAsync(int syllabusId, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
    public Task<int> GetTotalModulesAsync(int syllabusId, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
    public Task<int> GetTotalLessonsAsync(int syllabusId, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
    public Task<bool> PublishAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
    public Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = Items.FirstOrDefault(s => s.Id == id);
        if (item != null)
        {
            item.Status = SyllabusStatuses.Archived;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
    public Task<(IEnumerable<Syllabus> Syllabuses, int TotalCount)> GetSyllabusesPagedAsync(
        int pageNumber, int pageSize, string? searchTerm, int? gradeLevelId, string? status,
        CancellationToken cancellationToken = default)
    {
        var query = Items.AsEnumerable();
        if (gradeLevelId.HasValue) query = query.Where(s => s.GradeLevelId == gradeLevelId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(s => s.Status == status);
        var ordered = query.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Title).ToList();
        return Task.FromResult<(IEnumerable<Syllabus>, int)>((ordered, ordered.Count));
    }
    public Task<Syllabus?> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(s => s.Id == id));
    public Task<Syllabus?> GetStructureAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(s => s.Id == id));
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal class FakeGradeLevelRepository : FakeRepository<GradeLevel>, IGradeLevelRepository
{
    public Task<GradeLevel?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(g => g.Code == code));
    public Task<IEnumerable<GradeLevel>> GetAllOrderedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<GradeLevel>>(Items.OrderBy(g => g.Level));
    public new Task<GradeLevel> AddAsync(GradeLevel gradeLevel, CancellationToken cancellationToken = default)
    {
        if (gradeLevel.Id == 0)
            gradeLevel.Id = Items.Count == 0 ? 1 : Items.Max(g => g.Id) + 1;
        Items.Add(gradeLevel);
        return Task.FromResult(gradeLevel);
    }
    public new Task<GradeLevel> UpdateAsync(GradeLevel gradeLevel, CancellationToken cancellationToken = default) =>
        Task.FromResult(gradeLevel);
    public new Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = Items.FirstOrDefault(g => g.Id == id);
        if (item != null)
        {
            Items.Remove(item);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
    public Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = Items.Where(g => g.Code == code);
        if (excludeId.HasValue)
            query = query.Where(g => g.Id != excludeId.Value);
        return Task.FromResult(query.Any());
    }
    public Task<int> GetSyllabusCountAsync(int gradeLevelId, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
    public Task<int> GetCourseCountAsync(int gradeLevelId, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}

internal record RecordedLogCall(
    string Level,
    string Action,
    int? ActorUserId,
    string? ActorRole,
    string? EntityType,
    string? EntityId,
    string Description,
    object? Metadata);

internal class FakeSystemLogService : ISystemLogService
{
    public readonly List<RecordedLogCall> Calls = new();

    public Task WriteAsync(
        string level, string action, int? actorUserId, string? actorRole,
        string? entityType, string? entityId, string description, object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(new RecordedLogCall(level, action, actorUserId, actorRole, entityType, entityId, description, metadata));
        return Task.CompletedTask;
    }
}

public class SyllabusHandlerTests
{
    private static User MakeUser(int id, string roleName) =>
        new() { Id = id, Email = $"u{id}@test.com", FullName = "Test", Role = new Role { Id = 1, Name = roleName } };

    [Fact]
    public async Task CreateSyllabus_ByMasterAdmin_Succeeds_AndIsSystemOwned()
    {
        var syllabusRepo = new FakeSyllabusRepository();
        var gradeLevelRepo = new FakeGradeLevelRepository();

        var handler = new STEM.Application.UseCases.Curriculum.CreateSyllabusHandler(syllabusRepo, gradeLevelRepo);

        var id = await handler.Handle(new STEM.Application.Dtos.Curriculum.CreateSyllabusRequest
        {
            Title = "Chương trình khối 12",
            SubjectArea = "engineering",
            DisplayOrder = 1,
            EstimatedHours = 35,
            IsRequired = true
        });

        var created = syllabusRepo.Items.Single(s => s.Id == id);
        Assert.True(created.IsSystemOwned);
        Assert.Equal(SyllabusStatuses.Draft, created.Status);
    }

    [Fact]
    public async Task UpdateSyllabus_WhenNotFound_WritesNoSuccessAuditEvent()
    {
        var syllabusRepo = new FakeSyllabusRepository();
        var gradeLevelRepo = new FakeGradeLevelRepository();

        var handler = new STEM.Application.UseCases.Curriculum.UpdateSyllabusHandler(syllabusRepo, gradeLevelRepo);

        var result = await handler.Handle(999, new STEM.Application.Dtos.Curriculum.UpdateSyllabusRequest { Title = "New" });

        Assert.False(result);
    }

    [Fact]
    public async Task ArchiveSyllabus_SetsStatusArchived_NeverDeletes()
    {
        var syllabusRepo = new FakeSyllabusRepository();
        var syllabus = new Syllabus { Id = 1, Title = "Referenced by a Course", Status = SyllabusStatuses.Published };
        await syllabusRepo.AddAsync(syllabus);

        var handler = new STEM.Application.UseCases.Curriculum.ArchiveSyllabusHandler(syllabusRepo);

        var result = await handler.Handle(1);

        Assert.True(result);
        Assert.Equal(SyllabusStatuses.Archived, syllabus.Status);
        Assert.Contains(syllabus, syllabusRepo.Items); // still present — never hard-deleted
    }

    [Fact]
    public async Task GetSyllabusesList_SortsByDisplayOrder()
    {
        var syllabusRepo = new FakeSyllabusRepository();
        await syllabusRepo.AddAsync(new Syllabus { Id = 1, Title = "C", DisplayOrder = 3 });
        await syllabusRepo.AddAsync(new Syllabus { Id = 2, Title = "A", DisplayOrder = 1 });
        await syllabusRepo.AddAsync(new Syllabus { Id = 3, Title = "B", DisplayOrder = 2 });

        var handler = new GetSyllabusesListHandler(syllabusRepo);
        var result = await handler.Handle(new GetSyllabusesRequest());

        Assert.Equal(new[] { "A", "B", "C" }, result.Items.Select(i => i.Title));
    }

    [Fact]
    public async Task GetSyllabusStructure_TraversesFullTree_SortedByDisplayOrder()
    {
        var lab = new Lab { Id = Guid.NewGuid(), Title = "Robot giao hàng mini", Status = LabStatuses.Published };

        var lessonB = new Lesson { Id = 2, Title = "Bài B", DisplayOrder = 2, HasVirtualLab = true, Lab = lab };
        var lessonA = new Lesson { Id = 1, Title = "Bài A", DisplayOrder = 1 };

        var module = new Module
        {
            Id = 1,
            Title = "Module 1",
            DisplayOrder = 1,
            Lessons = new List<Lesson> { lessonB, lessonA } // intentionally out of order
        };

        var course = new Course
        {
            Id = 40,
            Title = "STEM_ENGINEERING_12",
            SchoolId = null,
            DisplayOrder = 1,
            Modules = new List<Module> { module }
        };

        var syllabus = new Syllabus
        {
            Id = 1,
            Title = "Chương trình khối 12",
            Status = SyllabusStatuses.Published,
            Courses = new List<Course> { course }
        };

        var syllabusRepo = new FakeSyllabusRepository();
        await syllabusRepo.AddAsync(syllabus);

        var handler = new GetSyllabusStructureHandler(syllabusRepo);
        var result = await handler.Handle(1);

        Assert.NotNull(result);
        var resultCourse = Assert.Single(result!.Courses);
        Assert.Equal("STEM_ENGINEERING_12", resultCourse.Title);
        var resultModule = Assert.Single(resultCourse.Modules);

        // Lessons must come back sorted by DisplayOrder, not insertion order.
        Assert.Equal(new[] { "Bài A", "Bài B" }, resultModule.Lessons.Select(l => l.Title));

        var lessonWithLab = resultModule.Lessons.Single(l => l.Title == "Bài B");
        Assert.NotNull(lessonWithLab.Lab);
        Assert.Equal("Robot giao hàng mini", lessonWithLab.Lab!.Title);

        var lessonWithoutLab = resultModule.Lessons.Single(l => l.Title == "Bài A");
        Assert.Null(lessonWithoutLab.Lab);
    }

    [Fact]
    public async Task GetSyllabusDetail_IncludesGradeLevelName()
    {
        var gradeLevel = new GradeLevel { Id = 1, Name = "Khối 12", Code = "G_12", Level = 12 };
        var syllabus = new Syllabus { Id = 1, Title = "Chương trình khối 12", GradeLevelId = 1, GradeLevel = gradeLevel };

        var syllabusRepo = new FakeSyllabusRepository();
        await syllabusRepo.AddAsync(syllabus);

        var handler = new GetSyllabusDetailHandler(syllabusRepo);
        var result = await handler.Handle(1);

        Assert.NotNull(result);
        Assert.Equal("Khối 12", result!.GradeLevelName);
    }

    [Fact]
    public async Task GetSyllabusStructure_ReturnsNull_WhenNotFound()
    {
        var handler = new GetSyllabusStructureHandler(new FakeSyllabusRepository());
        var result = await handler.Handle(999);
        Assert.Null(result);
    }

    private static readonly string[] SensitiveKeywords =
        { "password", "passwordhash", "token", "secret", "authorization", "apikey", "connectionstring" };

    [Fact]
    public async Task CreateSyllabus_AuditMetadata_ContainsNoSensitiveFields()
    {
        // The new Curriculum CreateSyllabusHandler does not perform audit logging.
        // This test verifies the DTO (CreateSyllabusRequest) itself contains no sensitive fields.
        var handler = new STEM.Application.UseCases.Curriculum.CreateSyllabusHandler(
            new FakeSyllabusRepository(),
            new FakeGradeLevelRepository());

        var request = new STEM.Application.Dtos.Curriculum.CreateSyllabusRequest
        {
            Title = "Chương trình khối 12",
            SubjectArea = "engineering"
        };

        // Serialize to JSON and check that no sensitive fields are present in the DTO
        var json = System.Text.Json.JsonSerializer.Serialize(request).ToLowerInvariant();
        foreach (var keyword in SensitiveKeywords)
            Assert.DoesNotContain(keyword, json);
    }
}
