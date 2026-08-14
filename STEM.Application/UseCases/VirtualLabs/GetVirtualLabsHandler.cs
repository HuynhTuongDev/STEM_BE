using STEM.Application.Dtos.VirtualLabs;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.VirtualLabs;

public class GetVirtualLabsHandler
{
    private readonly ISimulationRepository _simulationRepository;
    private readonly IUserRepository _userRepository;

    public GetVirtualLabsHandler(
        ISimulationRepository simulationRepository,
        IUserRepository userRepository)
    {
        _simulationRepository = simulationRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedVirtualLabResponse> Handle(
        GetVirtualLabsRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        int? schoolId = null;
        int? teacherId = null;
        int? studentId = null;
        var roleName = currentUser.Role?.Name;

        if (roleName == RoleNames.SchoolAdministrator)
        {
            schoolId = currentUser.SchoolId ?? throw new UnauthorizedAccessException("School admin has no school.");
        }
        else if (roleName == RoleNames.Teacher)
        {
            teacherId = currentUser.Id;
        }
        else if (roleName == RoleNames.Student)
        {
            studentId = currentUser.Id;
        }
        else
        {
            throw new UnauthorizedAccessException("You are not allowed to view virtual labs.");
        }

        var (templates, totalCount) = await _simulationRepository.GetTemplatesPagedAsync(
            pageNumber,
            pageSize,
            request.SearchTerm,
            request.ClassId,
            request.CourseId,
            schoolId,
            teacherId,
            studentId,
            cancellationToken);

        return new PagedVirtualLabResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = templates.Select(VirtualLabResponseMapper.Map).ToList()
        };
    }

    public async Task<PagedVirtualLabResponse> HandleForStudent(
        int studentId,
        int pageNumber,
        int pageSize,
        int? classId,
        CancellationToken cancellationToken = default)
    {
        var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            throw new UnauthorizedAccessException("Student not found.");
        }

        if (student.Role?.Name != RoleNames.Student)
        {
            throw new UnauthorizedAccessException("Only students can access this endpoint.");
        }

        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        // Get student's enrolled classes
        var enrollments = await _simulationRepository.GetStudentEnrollmentsAsync(studentId, cancellationToken);
        var enrolledClassIds = enrollments.Select(e => e.ClassId).ToList();

        // DEBUG: Log enrollment info
        Console.WriteLine($"[DEBUG] Student {studentId} enrolled in {enrolledClassIds.Count} classes: {string.Join(", ", enrolledClassIds)}");

        if (classId.HasValue && !enrolledClassIds.Contains(classId.Value))
        {
            throw new UnauthorizedAccessException("Student is not enrolled in this class.");
        }

        var (templates, totalCount) = await _simulationRepository.GetTemplatesPagedAsync(
            pageNumber,
            pageSize,
            null,
            classId,
            null,
            student.SchoolId,
            null,
            studentId,
            cancellationToken);

        // DEBUG: Log template count
        Console.WriteLine($"[DEBUG] Found {totalCount} templates for student {studentId}");

        // Get student's submissions for these labs
        var submissions = await _simulationRepository.GetStudentSubmissionsAsync(studentId, cancellationToken);
        var submissionMap = submissions.ToDictionary(s => s.TemplateId);

        var items = templates.Select(t =>
        {
            var item = VirtualLabResponseMapper.Map(t);
            // Override status based on submission
            if (submissionMap.ContainsKey(t.Id))
            {
                item.Status = "completed";
            }
            else
            {
                item.Status = "not_started";
            }
            return item;
        }).ToList();

        return new PagedVirtualLabResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        };
    }
}
