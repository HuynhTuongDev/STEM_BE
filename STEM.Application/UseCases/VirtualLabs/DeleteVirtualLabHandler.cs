using STEM.Core.Repository;

namespace STEM.Application.UseCases.VirtualLabs;

public class DeleteVirtualLabHandler
{
    private readonly ISimulationRepository _simulationRepository;
    private readonly IUserRepository _userRepository;

    public DeleteVirtualLabHandler(
        ISimulationRepository simulationRepository,
        IUserRepository userRepository)
    {
        _simulationRepository = simulationRepository;
        _userRepository = userRepository;
    }

    public async Task Handle(
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

        if (!VirtualLabAuthorization.CanManageLab(currentUser, lab))
        {
            throw new UnauthorizedAccessException("You are not allowed to delete this virtual lab.");
        }

        if (lab.Simulation != null && lab.Simulation.SimulationTemplates.Count <= 1)
        {
            _simulationRepository.DeleteSimulation(lab.Simulation);
        }
        else
        {
            _simulationRepository.Delete(lab);
        }

        await _simulationRepository.SaveChangesAsync(cancellationToken);
    }
}
