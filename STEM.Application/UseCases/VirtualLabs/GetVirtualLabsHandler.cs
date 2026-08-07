using STEM.Application.Dtos.VirtualLabs;
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
}
