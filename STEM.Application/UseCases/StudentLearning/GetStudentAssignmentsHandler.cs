using STEM.Application.Dtos.StudentLearning;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.StudentLearning;

public class GetStudentAssignmentsHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IUserRepository _userRepository;

    public GetStudentAssignmentsHandler(
        IAssignmentRepository assignmentRepository,
        IUserRepository userRepository)
    {
        _assignmentRepository = assignmentRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedStudentAssignmentResponse> Handle(
        GetStudentAssignmentsRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        StudentLearningGuard.EnsureStudent(currentUser);

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var (assignments, totalCount) = await _assignmentRepository.GetStudentAssignmentsPagedAsync(
            currentUserId,
            pageNumber,
            pageSize,
            request.ClassId,
            request.SearchTerm,
            cancellationToken);

        return new PagedStudentAssignmentResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = assignments
                .Select(a => StudentLearningMapper.ToAssignmentResponse(a, currentUserId))
                .ToList()
        };
    }
}
