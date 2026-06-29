using STEM.Application.Dtos.VirtualLabs;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Simulations;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.VirtualLabs;

public class CreateVirtualLabHandler
{
    private readonly ISimulationRepository _simulationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWokwiService _wokwiService;

    public CreateVirtualLabHandler(
        ISimulationRepository simulationRepository,
        IUserRepository userRepository,
        IWokwiService wokwiService)
    {
        _simulationRepository = simulationRepository;
        _userRepository = userRepository;
        _wokwiService = wokwiService;
    }

    public async Task<VirtualLabResponse> Handle(
        CreateVirtualLabRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.LessonId, request.SimulationName, request.DiagramJson);
        ValidateDiagram(request.DiagramJson);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var lesson = await _simulationRepository.GetLessonWithDetailsAsync(request.LessonId, cancellationToken);
        if (lesson == null)
        {
            throw new KeyNotFoundException("Lesson not found.");
        }

        if (!VirtualLabAuthorization.CanManageLesson(currentUser, lesson))
        {
            throw new UnauthorizedAccessException("You are not allowed to create virtual labs for this lesson.");
        }

        var now = DateTime.UtcNow;
        var template = new SimulationTemplate
        {
            SimulationName = request.SimulationName.Trim(),
            Description = request.Description.Trim(),
            Config = request.DiagramJson,
            CreatedAt = now,
            UpdatedAt = now,
            Simulation = new SimulationEntity
            {
                LessonId = request.LessonId,
                Lesson = lesson,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        await _simulationRepository.AddAsync(template, cancellationToken);
        await _simulationRepository.SaveChangesAsync(cancellationToken);

        return VirtualLabResponseMapper.Map(template);
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

    internal static void ValidateRequest(int lessonId, string simulationName, string diagramJson)
    {
        if (lessonId <= 0)
        {
            throw new ArgumentException("LessonId is required.");
        }

        if (string.IsNullOrWhiteSpace(simulationName))
        {
            throw new ArgumentException("SimulationName is required.");
        }

        if (string.IsNullOrWhiteSpace(diagramJson))
        {
            throw new ArgumentException("DiagramJson is required.");
        }
    }
}
