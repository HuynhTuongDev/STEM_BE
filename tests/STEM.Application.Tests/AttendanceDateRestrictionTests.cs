using System.Linq.Expressions;
using STEM.Application.Dtos.Attendance;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Attendance;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.Tests;

file sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider(DateTime utcNow) => UtcNow = utcNow;
    public DateTime UtcNow { get; }
}

file sealed class FakeUserRepository : IUserRepository
{
    private readonly User _user;
    public FakeUserRepository(User user) => _user = user;

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(id == _user.Id ? _user : null);

    public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task AddAsync(User entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task AddRangeAsync(IEnumerable<User> entities, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public void Update(User entity) => throw new NotImplementedException();
    public void Delete(User entity) => throw new NotImplementedException();
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteAsync(User entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<(IEnumerable<User> Users, int TotalCount)> GetUsersPagedAsync(int pageNumber, int pageSize, string? searchTerm, int? roleId, bool? isActive, int? schoolId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<User>> GetStudentsNotInClassAsync(int classId, int schoolId, string? searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<Schedule>> GetStudentSchedulesAsync(int studentId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<(IEnumerable<User> Users, int TotalCount)> GetTeachersWithClassCountAsync(int schoolId, int page, int pageSize, string? searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<User>> GetBySchoolIdAsync(int schoolId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<(int TeacherCount, int StudentCount)> GetTeacherStudentCountBySchoolAsync(int schoolId, CancellationToken cancellationToken = default) => Task.FromResult<(int, int)>((0, 0));
}

file sealed class FakeClassRepository : IClassRepository
{
    private readonly Class _class;
    public FakeClassRepository(Class classEntity) => _class = classEntity;

    public Task<Class?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult<Class?>(id == _class.Id ? _class : null);

    public Task<Class?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<Class>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<Class>> FindAsync(Expression<Func<Class, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task AddAsync(Class entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task AddRangeAsync(IEnumerable<Class> entities, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public void Update(Class entity) => throw new NotImplementedException();
    public void Delete(Class entity) => throw new NotImplementedException();
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteAsync(Class entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<Class>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<Class>> GetByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<Class>> GetClassesByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<(IEnumerable<Class> Classes, int TotalCount)> GetClassesPagedAsync(int pageNumber, int pageSize, string? searchTerm, int? courseId, int? teacherId, int? schoolId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<Schedule>> GetSchedulesByTeacherAsync(int teacherId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<Class?> GetByIdSummaryAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<Schedule>> GetSchedulesAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<List<int>> GetAvailableTeacherIdsForClassAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<Assignment>> GetClassAssignmentsAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<STEM.Core.Entities.Courses.Course?> GetCourseByIdAsync(int courseId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

file sealed class FakeAttendanceRepository : IAttendanceRepository
{
    private readonly List<AttendanceRecord> _records;
    public FakeAttendanceRepository(IEnumerable<AttendanceRecord>? seed = null) => _records = seed?.ToList() ?? new List<AttendanceRecord>();

    public Task<AttendanceRecord?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_records.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyCollection<AttendanceRecord>> GetByClassDateAsync(int classId, DateOnly attendanceDate, int? scheduleId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<AttendanceRecord>>(
            _records.Where(r => r.ClassId == classId && r.AttendanceDate == attendanceDate && (scheduleId == null || r.ScheduleId == scheduleId)).ToList());

    public Task<(IEnumerable<AttendanceRecord> Records, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, int? classId, int? studentId, DateOnly? attendanceDate, int? schoolId, int? teacherId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<AttendanceRecord?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<AttendanceRecord>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<AttendanceRecord>> FindAsync(Expression<Func<AttendanceRecord, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task AddAsync(AttendanceRecord entity, CancellationToken cancellationToken = default) { _records.Add(entity); return Task.CompletedTask; }
    public Task AddRangeAsync(IEnumerable<AttendanceRecord> entities, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public void Update(AttendanceRecord entity) { }
    public void Delete(AttendanceRecord entity) => throw new NotImplementedException();
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteAsync(AttendanceRecord entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task DeleteByScheduleIdAsync(int scheduleId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

public class AttendanceDateRestrictionTests
{
    private static readonly DateTime BusinessNow = new(2026, 8, 11, 3, 0, 0, DateTimeKind.Utc); // "today" = 2026-08-11
    private static readonly DateOnly Today = DateOnly.FromDateTime(BusinessNow);
    private static readonly DateOnly Yesterday = Today.AddDays(-1);
    private static readonly DateOnly Tomorrow = Today.AddDays(1);

    private static User MakeTeacher(int id = 11) => new()
    {
        Id = id,
        FullName = "Teacher",
        Role = new Role { Id = 3, Name = RoleNames.Teacher }
    };

    private static User MakeSchoolAdmin(int id = 5, int schoolId = 17) => new()
    {
        Id = id,
        SchoolId = schoolId,
        FullName = "Admin",
        Role = new Role { Id = 2, Name = RoleNames.SchoolAdministrator }
    };

    private static Class MakeClass(int id, int teacherId, int schoolId, params (int StudentId, User Student)[] students)
    {
        var classEntity = new Class { Id = id, TeacherId = teacherId, SchoolId = schoolId };
        classEntity.Enrollments = students
            .Select(s => new Enrollment { ClassId = id, StudentId = s.StudentId, Student = s.Student, Class = classEntity })
            .ToList();
        return classEntity;
    }

    private static CreateAttendanceHandler MakeCreateHandler(Class classEntity, User currentUser, DateTime businessNow, IEnumerable<AttendanceRecord>? existing = null) =>
        new(
            new FakeAttendanceRepository(existing),
            new FakeClassRepository(classEntity),
            new FakeUserRepository(currentUser),
            new FakeDateTimeProvider(businessNow));

    [Fact]
    public async Task Create_Teacher_AttendanceDateToday_IsAllowed()
    {
        var teacher = MakeTeacher();
        var student = new User { Id = 33, FullName = "Student" };
        var classEntity = MakeClass(4, teacher.Id, schoolId: 17, (33, student));
        var handler = MakeCreateHandler(classEntity, teacher, BusinessNow);

        var request = new CreateAttendanceRequest
        {
            ClassId = 4,
            AttendanceDate = Today,
            Records = new() { new CreateAttendanceRecordRequest { StudentId = 33, Status = "Present" } }
        };

        var response = await handler.Handle(request, teacher.Id);

        Assert.Equal(1, response.CreatedCount);
    }

    [Fact]
    public async Task Create_Teacher_AttendanceDatePast_IsRejected()
    {
        var teacher = MakeTeacher();
        var student = new User { Id = 33, FullName = "Student" };
        var classEntity = MakeClass(4, teacher.Id, schoolId: 17, (33, student));
        var handler = MakeCreateHandler(classEntity, teacher, BusinessNow);

        var request = new CreateAttendanceRequest
        {
            ClassId = 4,
            AttendanceDate = Yesterday,
            Records = new() { new CreateAttendanceRecordRequest { StudentId = 33, Status = "Present" } }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(request, teacher.Id));
    }

    [Fact]
    public async Task Create_Teacher_AttendanceDateFuture_IsRejected()
    {
        var teacher = MakeTeacher();
        var student = new User { Id = 33, FullName = "Student" };
        var classEntity = MakeClass(4, teacher.Id, schoolId: 17, (33, student));
        var handler = MakeCreateHandler(classEntity, teacher, BusinessNow);

        var request = new CreateAttendanceRequest
        {
            ClassId = 4,
            AttendanceDate = Tomorrow,
            Records = new() { new CreateAttendanceRecordRequest { StudentId = 33, Status = "Present" } }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(request, teacher.Id));
    }

    [Fact]
    public async Task Create_SchoolAdmin_AttendanceDatePast_IsNotBlockedByDateRule()
    {
        var admin = MakeSchoolAdmin();
        var student = new User { Id = 33, FullName = "Student" };
        var classEntity = MakeClass(4, teacherId: 99, schoolId: admin.SchoolId!.Value, (33, student));
        var handler = MakeCreateHandler(classEntity, admin, BusinessNow);

        var request = new CreateAttendanceRequest
        {
            ClassId = 4,
            AttendanceDate = Yesterday,
            Records = new() { new CreateAttendanceRecordRequest { StudentId = 33, Status = "Present" } }
        };

        // SchoolAdministrator is out of scope for this rule; should reach normal success path.
        var response = await handler.Handle(request, admin.Id);

        Assert.Equal(1, response.CreatedCount);
    }

    [Fact]
    public async Task Update_Teacher_ExistingRecordToday_IsAllowed()
    {
        var teacher = MakeTeacher();
        var classEntity = MakeClass(4, teacher.Id, schoolId: 17);
        var record = new AttendanceRecord { Id = 10, ClassId = 4, StudentId = 33, AttendanceDate = Today, Status = "Present", Class = classEntity };
        var handler = new UpdateAttendanceHandler(
            new FakeAttendanceRepository(new[] { record }),
            new FakeUserRepository(teacher),
            new FakeDateTimeProvider(BusinessNow));

        var response = await handler.Handle(10, new UpdateAttendanceRequest { Status = "Absent" }, teacher.Id);

        Assert.Equal("Absent", response.Status);
    }

    [Fact]
    public async Task Update_Teacher_ExistingRecordPast_IsRejected()
    {
        var teacher = MakeTeacher();
        var classEntity = MakeClass(4, teacher.Id, schoolId: 17);
        var record = new AttendanceRecord { Id = 10, ClassId = 4, StudentId = 33, AttendanceDate = Yesterday, Status = "Present", Class = classEntity };
        var handler = new UpdateAttendanceHandler(
            new FakeAttendanceRepository(new[] { record }),
            new FakeUserRepository(teacher),
            new FakeDateTimeProvider(BusinessNow));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(10, new UpdateAttendanceRequest { Status = "Absent" }, teacher.Id));
    }

    [Fact]
    public async Task Update_Teacher_ExistingRecordFuture_IsRejected()
    {
        var teacher = MakeTeacher();
        var classEntity = MakeClass(4, teacher.Id, schoolId: 17);
        var record = new AttendanceRecord { Id = 10, ClassId = 4, StudentId = 33, AttendanceDate = Tomorrow, Status = "Present", Class = classEntity };
        var handler = new UpdateAttendanceHandler(
            new FakeAttendanceRepository(new[] { record }),
            new FakeUserRepository(teacher),
            new FakeDateTimeProvider(BusinessNow));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(10, new UpdateAttendanceRequest { Status = "Absent" }, teacher.Id));
    }

    [Fact]
    public async Task Update_SchoolAdmin_ExistingRecordPast_IsNotBlockedByDateRule()
    {
        var admin = MakeSchoolAdmin();
        var classEntity = MakeClass(4, teacherId: 99, schoolId: admin.SchoolId!.Value);
        var record = new AttendanceRecord { Id = 10, ClassId = 4, StudentId = 33, AttendanceDate = Yesterday, Status = "Present", Class = classEntity };
        var handler = new UpdateAttendanceHandler(
            new FakeAttendanceRepository(new[] { record }),
            new FakeUserRepository(admin),
            new FakeDateTimeProvider(BusinessNow));

        var response = await handler.Handle(10, new UpdateAttendanceRequest { Status = "Absent" }, admin.Id);

        Assert.Equal("Absent", response.Status);
    }
}
