using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using STEM.Application.Dtos.Grading;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services;

public class GradingSessionService : IGradingSessionService
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;
    private readonly StemDbContext _context;

    public GradingSessionService(
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository,
        StemDbContext context)
    {
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
        _context = context;
    }

    public static Guid DeriveGradingSessionId(int submissionId) =>
        Guid.Parse($"00000000-0000-4000-8000-{submissionId:x12}");

    public async Task<PrepareGradingSessionResponse> PrepareAsync(
        int submissionId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user not found.");

        var submission = await _submissionRepository.GetByIdWithDetailsAsync(submissionId, cancellationToken)
            ?? throw new KeyNotFoundException("Submission not found.");

        var classEntity = submission.Assignment?.Class;
        var roleName = currentUser.Role?.Name;
        var canManage = classEntity != null && roleName switch
        {
            RoleNames.Teacher => classEntity.TeacherId == currentUser.Id,
            RoleNames.SchoolAdministrator => currentUser.SchoolId.HasValue && currentUser.SchoolId.Value == classEntity.SchoolId,
            _ => false,
        };

        if (!canManage)
        {
            throw new UnauthorizedAccessException("You are not allowed to run this submission.");
        }

        using var doc = JsonDocument.Parse(submission.ContentJson);
        if (!doc.RootElement.TryGetProperty("virtualLabSubmission", out var vl))
        {
            throw new InvalidOperationException("Submission is not a virtual lab submission.");
        }

        var diagram = vl.GetProperty("diagram");
        var board = diagram.TryGetProperty("board", out var boardEl) && boardEl.ValueKind == JsonValueKind.String
            ? boardEl.GetString()!
            : "esp32";
        var sourceCode = vl.TryGetProperty("sourceCode", out var codeEl) ? codeEl.GetString() ?? string.Empty : string.Empty;
        var diagramJson = diagram.GetRawText();

        var sessionId = DeriveGradingSessionId(submissionId);
        var now = DateTime.UtcNow;

        var project = await _context.VirtualLabProjects.FirstOrDefaultAsync(item => item.Id == sessionId, cancellationToken);
        if (project == null)
        {
            var name = $"grading-{submissionId}";
            project = new VirtualLabProject
            {
                Id = sessionId,
                UserId = currentUserId,
                Name = name.Length > 20 ? name[..20] : name,
                Board = board,
                Language = "arduino",
                DiagramJson = diagramJson,
                CodeContent = sourceCode,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _context.VirtualLabProjects.Add(project);
        }
        else
        {
            project.Board = board;
            project.DiagramJson = diagramJson;
            project.CodeContent = sourceCode;
            project.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new PrepareGradingSessionResponse { SessionId = sessionId.ToString("N") };
    }
}
