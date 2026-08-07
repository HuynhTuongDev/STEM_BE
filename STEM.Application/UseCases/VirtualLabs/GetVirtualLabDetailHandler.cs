using STEM.Application.Dtos.VirtualLabs;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.VirtualLabs;

public class GetVirtualLabDetailHandler
{
    private readonly ISimulationRepository _simulationRepository;
    private readonly IUserRepository _userRepository;

    public GetVirtualLabDetailHandler(
        ISimulationRepository simulationRepository,
        IUserRepository userRepository)
    {
        _simulationRepository = simulationRepository;
        _userRepository = userRepository;
    }

    public async Task<VirtualLabResponse> Handle(
        int labId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var lab = await _simulationRepository.GetTemplateWithDetailsAsync(labId, cancellationToken);
        if (lab == null)
        {
            throw new KeyNotFoundException("Virtual lab not found.");
        }

        if (!VirtualLabAuthorization.CanViewLab(currentUser, lab))
        {
            throw new UnauthorizedAccessException("You are not allowed to view this virtual lab.");
        }

        return VirtualLabResponseMapper.Map(lab);
    }
}
