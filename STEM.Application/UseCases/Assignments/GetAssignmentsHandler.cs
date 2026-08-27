using System.Security.Claims;
using STEM.Application.Dtos.Assignments;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Assignments;

public class GetAssignmentsHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IUserRepository _userRepository;

    public GetAssignmentsHandler(
        IAssignmentRepository assignmentRepository,
        IUserRepository userRepository)
    {
        _assignmentRepository = assignmentRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedAssignmentResponse> Handle(
        GetAssignmentsRequest request,
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
        var studentId = request.StudentId;
        
        // Get role from claims (more reliable than loading from DB)
        var roleName = currentUser.Role?.Name 
            ?? RoleNames.Student; // Default to Student for enrolled users

        if (roleName == RoleNames.SchoolAdministrator)
        {
            schoolId = currentUser.SchoolId ?? throw new UnauthorizedAccessException("School admin has no school.");
        }
        else if (roleName == RoleNames.Teacher)
        {
            teacherId = currentUser.Id;
        }
        else // Student (default)
        {
            if (request.StudentId.HasValue && request.StudentId.Value != currentUser.Id)
            {
                throw new UnauthorizedAccessException("Students can only view their own assignments.");
            }

            studentId = currentUser.Id;
        }

        var (assignments, totalCount) = await _assignmentRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            request.SearchTerm,
            request.ClassId,
            request.CourseId,
            schoolId,
            teacherId,
            studentId,
            cancellationToken);

        var revealAnswers = roleName != RoleNames.Student;

        return new PagedAssignmentResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = assignments.Select(assignment => AssignmentResponseMapper.Map(assignment, revealAnswers: revealAnswers, currentStudentId: studentId)).ToList()
        };
    }
}
