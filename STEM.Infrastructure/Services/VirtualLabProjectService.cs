using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using STEM.Application.Dtos.VirtualLab;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Simulation;
using STEM.Core.Entities.Simulations;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services;

public class VirtualLabProjectService : IVirtualLabProjectService
{
    private readonly StemDbContext _context;
    private readonly VirtualLabDiagramService _diagramService;

    public VirtualLabProjectService(StemDbContext context, VirtualLabDiagramService diagramService)
    {
        _context = context;
        _diagramService = diagramService;
    }

    public async Task<VirtualLabProject> CreateProjectAsync(VirtualLabProjectRequest request, int userId)
    {
        var diagramJson = ToDiagramJson(request.Diagram);
        var analysis = _diagramService.Analyze(diagramJson);
        if (!analysis.Validation.IsValid)
        {
            throw new ArgumentException(string.Join("; ", analysis.Validation.Errors));
        }

        var project = new VirtualLabProject
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Board = request.Board,
            Language = request.Language,
            CodeContent = request.Code,
            DiagramJson = analysis.DiagramJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.VirtualLabProjects.Add(project);
        await _context.SaveChangesAsync();

        return project;
    }

    public async Task<VirtualLabProject?> GetProjectAsync(Guid id, int currentUserId)
    {
        var project = await _context.VirtualLabProjects.FindAsync(id);
        if (project == null) return null;

        EnsureOwnership(project, currentUserId);
        return project;
    }

    public async Task<VirtualLabProject?> UpdateProjectAsync(Guid id, VirtualLabProjectRequest request, int currentUserId)
    {
        var project = await _context.VirtualLabProjects.FindAsync(id);
        if (project == null) return null;

        EnsureOwnership(project, currentUserId);

        var diagramJson = ToDiagramJson(request.Diagram);
        var analysis = _diagramService.Analyze(diagramJson);
        if (!analysis.Validation.IsValid)
        {
            throw new ArgumentException(string.Join("; ", analysis.Validation.Errors));
        }

        project.Name = request.Name;
        project.Board = request.Board;
        project.Language = request.Language;
        project.CodeContent = request.Code;
        project.DiagramJson = analysis.DiagramJson;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return project;
    }

    private static void EnsureOwnership(VirtualLabProject project, int currentUserId)
    {
        if (project.UserId.HasValue && project.UserId != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not allowed to access this virtual lab project.");
        }
    }

    private static string ToDiagramJson(JsonElement diagram)
    {
        if (diagram.ValueKind == JsonValueKind.String)
        {
            return diagram.GetString() ?? "{}";
        }

        return diagram.ValueKind == JsonValueKind.Undefined
            ? "{}"
            : diagram.GetRawText();
    }
}
