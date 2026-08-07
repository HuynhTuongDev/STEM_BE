using STEM.Application.Dtos.VirtualLabs;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.VirtualLabs;

public class CreateVirtualLabHandler
{
    private readonly ISimulationRepository _simulationRepository;
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWokwiService _wokwiService;

    public CreateVirtualLabHandler(
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
        CreateVirtualLabRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.ClassId, request.SimulationName, request.DiagramJson);
        ValidateDiagram(request.DiagramJson);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var classEntity = await _classRepository.GetByIdWithDetailsAsync(request.ClassId, cancellationToken);
        if (classEntity == null)
        {
            throw new KeyNotFoundException("Class not found.");
        }

        if (!VirtualLabAuthorization.CanManageClass(currentUser, classEntity))
        {
            throw new UnauthorizedAccessException("You are not allowed to create virtual labs for this class.");
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
                ClassId = request.ClassId,
                Class = classEntity,
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

    internal static void ValidateRequest(int classId, string simulationName, string diagramJson)
    {
        if (classId <= 0)
        {
            throw new ArgumentException("ClassId is required.");
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
