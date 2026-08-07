using STEM.Application.Dtos.VirtualLabs;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.VirtualLabs;

public class UpdateVirtualLabHandler
{
    private readonly ISimulationRepository _simulationRepository;
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWokwiService _wokwiService;

    public UpdateVirtualLabHandler(
        ISimulationRepository simulationRepository,
        IClassRepository classRepository,
        IUserRepository userRepository,
        IWokwiService wokwiService)
    {
        _simulationRepository = simulationRepository;
        _classRepository = classRepository;
        _userRepository = userRepository;
        _wokwiService = wokwiService;
    }

    public async Task<VirtualLabResponse> Handle(
        int labId,
        UpdateVirtualLabRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        CreateVirtualLabHandler.ValidateRequest(request.ClassId, request.SimulationName, request.DiagramJson);
        ValidateDiagram(request.DiagramJson);

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
            throw new UnauthorizedAccessException("You are not allowed to update this virtual lab.");
        }

        var targetClass = await _classRepository.GetByIdWithDetailsAsync(request.ClassId, cancellationToken);
        if (targetClass == null)
        {
            throw new KeyNotFoundException("Class not found.");
        }

        if (!VirtualLabAuthorization.CanManageClass(currentUser, targetClass))
        {
            throw new UnauthorizedAccessException("You are not allowed to move this virtual lab to the selected class.");
        }

        if (lab.Simulation == null)
        {
            throw new KeyNotFoundException("Virtual lab simulation not found.");
        }

        var now = DateTime.UtcNow;
        lab.SimulationName = request.SimulationName.Trim();
        lab.Description = request.Description.Trim();
        lab.Config = request.DiagramJson;
        lab.UpdatedAt = now;
        lab.Simulation.ClassId = request.ClassId;
        lab.Simulation.Class = targetClass;
        lab.Simulation.UpdatedAt = now;

        _simulationRepository.Update(lab);
        await _simulationRepository.SaveChangesAsync(cancellationToken);

        return VirtualLabResponseMapper.Map(lab);
    }

    private void ValidateDiagram(string diagramJson)
    {
        var diagram = _wokwiService.ParseDiagram(diagramJson);
        var (isValid, errors) = _wokwiService.ValidateDiagram(diagram);
        if (!isValid)
        {
            throw new ArgumentException($"Diagram is invalid: {string.Join("; ", errors)}");
        }
    }
}
