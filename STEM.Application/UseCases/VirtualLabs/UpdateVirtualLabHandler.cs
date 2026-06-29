using STEM.Application.Dtos.VirtualLabs;
using STEM.Application.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.VirtualLabs;

public class UpdateVirtualLabHandler
{
    private readonly ISimulationRepository _simulationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWokwiService _wokwiService;

    public UpdateVirtualLabHandler(
        ISimulationRepository simulationRepository,
        IUserRepository userRepository,
        IWokwiService wokwiService)
    {
        _simulationRepository = simulationRepository;
        _userRepository = userRepository;
        _wokwiService = wokwiService;
    }

    public async Task<VirtualLabResponse> Handle(
        int labId,
        UpdateVirtualLabRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        CreateVirtualLabHandler.ValidateRequest(request.LessonId, request.SimulationName, request.DiagramJson);
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

        var targetLesson = await _simulationRepository.GetLessonWithDetailsAsync(request.LessonId, cancellationToken);
        if (targetLesson == null)
        {
            throw new KeyNotFoundException("Lesson not found.");
        }

        if (!VirtualLabAuthorization.CanManageLesson(currentUser, targetLesson))
        {
            throw new UnauthorizedAccessException("You are not allowed to move this virtual lab to the selected lesson.");
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
        lab.Simulation.LessonId = request.LessonId;
        lab.Simulation.Lesson = targetLesson;
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
