using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Simulation;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Simulations;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services;

public class VirtualLabRuntimeService : IVirtualLabRuntimeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly StemDbContext _context;
    private readonly VirtualLabDiagramService _diagramService;
    private readonly VirtualLabMockRunner _mockRunner;

    public VirtualLabRuntimeService(
        StemDbContext context,
        VirtualLabDiagramService diagramService,
        VirtualLabMockRunner mockRunner)
    {
        _context = context;
        _diagramService = diagramService;
        _mockRunner = mockRunner;
    }

    public async Task<DiagramSessionResponse?> GetDiagramAsync(
        string sessionId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var projectId))
        {
            return null;
        }

        var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: true, cancellationToken);
        if (project == null)
        {
            return null;
        }

        var analysis = _diagramService.Analyze(project.DiagramJson);
        return BuildDiagramResponse(sessionId, analysis, project.UpdatedAt);
    }

    public async Task<DiagramSessionResponse> SaveDiagramAsync(
        string sessionId,
        SaveDiagramRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var analysis = _diagramService.Analyze(request.DiagramJson);

        if (!Guid.TryParse(sessionId, out var projectId))
        {
            throw new ArgumentException("sessionId must be a GUID virtual-lab project id.");
        }

        var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: false, cancellationToken);

        if (project == null)
        {
            project = new VirtualLabProject
            {
                Id = projectId,
                UserId = currentUserId,
                Name = $"session-{projectId:N}"[..20],
                Board = "esp32",
                Language = "arduino",
                DiagramJson = analysis.DiagramJson,
                CodeContent = request.SourceCode ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.VirtualLabProjects.Add(project);
        }
        else
        {
            project.DiagramJson = analysis.DiagramJson;
            if (request.SourceCode != null)
            {
                project.CodeContent = request.SourceCode;
            }

            project.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return BuildDiagramResponse(sessionId, analysis, project.UpdatedAt);
    }

    public async Task<RunEsp32SimulationResponse> RunEsp32Async(
        RunEsp32SimulationRequest request,
        int? currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (!request.Mode.Equals("mock", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only mock mode is supported by the current MVP runner.");
        }

        var sessionId = string.IsNullOrWhiteSpace(request.SessionId)
            ? Guid.NewGuid().ToString("N")
            : request.SessionId.Trim();

        var diagramJson = !string.IsNullOrWhiteSpace(request.DiagramJson)
            ? request.DiagramJson
            : await ResolveDiagramJsonAsync(sessionId, currentUserId, cancellationToken) ?? string.Empty;

        var sourceCode = !string.IsNullOrWhiteSpace(request.SourceCode)
            ? request.SourceCode
            : await ResolveSourceCodeAsync(sessionId, currentUserId, cancellationToken) ?? string.Empty;

        var analysis = _diagramService.Analyze(diagramJson);
        var events = _mockRunner.Run(sourceCode, analysis.DiagramJson, analysis);
        var hasErrors = !analysis.Validation.IsValid || events.Any(item => item.Type.Equals("error", StringComparison.OrdinalIgnoreCase));

        await PersistRunAsync(sessionId, analysis.DiagramJson, sourceCode, currentUserId, cancellationToken);

        return new RunEsp32SimulationResponse
        {
            SessionId = sessionId,
            Status = hasErrors ? "error" : "running",
            Validation = analysis.Validation,
            Netlist = analysis.Netlist,
            Events = events
        };
    }

    public async Task<VirtualLabSubmissionResponse> SubmitVirtualLabAsync(
        VirtualLabSubmissionRequest request,
        int? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _context.Assignments
            .Include(item => item.SimulationDetail)
            .FirstOrDefaultAsync(item => item.Id == request.AssignmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Assignment not found.");

        var studentId = currentUserId ?? request.StudentId
            ?? throw new UnauthorizedAccessException("Student id is required.");

        var analysis = _diagramService.Analyze(request.DiagramJson);
        var autoCheck = BuildAutoGradeResult(analysis, request);
        var autoScore = assignment.MaxScore * autoCheck.PassedChecks / Math.Max(autoCheck.TotalChecks, 1);

        var contentJson = JsonSerializer.Serialize(new
        {
            virtualLabSubmission = new
            {
                sessionId = request.SessionId,
                diagram = JsonSerializer.Deserialize<JsonElement>(analysis.DiagramJson),
                sourceCode = request.SourceCode,
                compileResult = request.CompileResult,
                simulationSummary = new
                {
                    eventCount = request.SimulationEvents.Count,
                    events = request.SimulationEvents
                }
            }
        }, JsonOptions);

        var submission = new Submission
        {
            AssignmentId = assignment.Id,
            StudentId = studentId,
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatuses.Submitted,
            ContentJson = contentJson,
            AutoGradeResultJson = JsonSerializer.Serialize(autoCheck, JsonOptions),
            AutoScore = autoScore,
            FinalScore = autoScore,
            AttemptNumber = await GetNextAttemptNumberAsync(assignment.Id, studentId, cancellationToken)
        };

        _context.Submissions.Add(submission);
        await _context.SaveChangesAsync(cancellationToken);

        return new VirtualLabSubmissionResponse
        {
            SubmissionId = submission.Id,
            Status = submission.Status,
            AutoScore = submission.AutoScore,
            AutoCheck = autoCheck
        };
    }

    private async Task PersistRunAsync(
        string sessionId,
        string diagramJson,
        string sourceCode,
        int? currentUserId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out var projectId))
        {
            throw new ArgumentException("sessionId must be a GUID virtual-lab project id.");
        }

        var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: false, cancellationToken);

        if (project == null)
        {
            project = new VirtualLabProject
            {
                Id = projectId,
                UserId = currentUserId,
                Name = $"session-{projectId:N}"[..20],
                Board = "esp32",
                Language = "arduino",
                DiagramJson = diagramJson,
                CodeContent = sourceCode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.VirtualLabProjects.Add(project);
        }
        else
        {
            project.DiagramJson = diagramJson;
            project.CodeContent = sourceCode;
            project.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<string?> ResolveDiagramJsonAsync(
        string sessionId,
        int? currentUserId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out var projectId))
        {
            return null;
        }

        var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: true, cancellationToken);
        return project?.DiagramJson;
    }

    private async Task<string?> ResolveSourceCodeAsync(
        string sessionId,
        int? currentUserId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out var projectId))
        {
            return null;
        }

        var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: true, cancellationToken);
        return project?.CodeContent;
    }

    /// <summary>
    /// Loads a project by id, or null if it doesn't exist. Throws UnauthorizedAccessException
    /// if the project has a recorded owner and it doesn't match currentUserId — the single
    /// ownership gate shared by every read/write path in this service.
    /// </summary>
    private async Task<VirtualLabProject?> LoadOwnedProjectAsync(
        Guid projectId,
        int? currentUserId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = _context.VirtualLabProjects.Where(item => item.Id == projectId);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var project = await query.FirstOrDefaultAsync(cancellationToken);
        if (project == null)
        {
            return null;
        }

        if (project.UserId.HasValue && project.UserId != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not allowed to access this virtual lab project.");
        }

        return project;
    }

    private static AutoGradeResultResponse BuildAutoGradeResult(
        VirtualLabDiagramAnalysis analysis,
        VirtualLabSubmissionRequest request)
    {
        var checks = new List<AutoGradeCheckResponse>
        {
            new()
            {
                Name = "diagram",
                Passed = analysis.Validation.IsValid,
                Message = analysis.Validation.IsValid
                    ? "Diagram validation passed."
                    : string.Join("; ", analysis.Validation.Errors)
            },
            new()
            {
                Name = "compile",
                Passed = request.CompileResult?.Success == true,
                Message = request.CompileResult?.Success == true
                    ? "Compile passed."
                    : "Compile result is missing or failed."
            },
            new()
            {
                Name = "behavior",
                Passed = request.SimulationEvents.Count > 0 &&
                         request.SimulationEvents.All(item => !item.Type.Equals("error", StringComparison.OrdinalIgnoreCase)),
                Message = request.SimulationEvents.Count > 0
                    ? "Simulation events were captured."
                    : "No simulation events were submitted."
            }
        };

        var passedChecks = checks.Count(item => item.Passed);
        return new AutoGradeResultResponse
        {
            Passed = passedChecks == checks.Count,
            PassedChecks = passedChecks,
            TotalChecks = checks.Count,
            Checks = checks
        };
    }

    private async Task<int> GetNextAttemptNumberAsync(
        int assignmentId,
        int studentId,
        CancellationToken cancellationToken)
    {
        var latestAttempt = await _context.Submissions
            .AsNoTracking()
            .Where(item => item.AssignmentId == assignmentId && item.StudentId == studentId)
            .Select(item => (int?)item.AttemptNumber)
            .MaxAsync(cancellationToken);

        return (latestAttempt ?? 0) + 1;
    }

    private static DiagramSessionResponse BuildDiagramResponse(
        string sessionId,
        VirtualLabDiagramAnalysis analysis,
        DateTime updatedAt)
    {
        return new DiagramSessionResponse
        {
            SessionId = sessionId,
            DiagramJson = analysis.DiagramJson,
            Validation = analysis.Validation,
            Netlist = analysis.Netlist,
            UpdatedAt = updatedAt
        };
    }
}
