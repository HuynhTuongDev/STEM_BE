using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using STEM.Application.Dtos.VirtualLab;
using STEM.Application.Interfaces;
using STEM.Application.Validators.VirtualLab;
using STEM.Core.Entities.Simulations;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services;

public class VirtualLabProjectService : IVirtualLabProjectService
{
    private readonly StemDbContext _context;
    private readonly DiagramValidator _validator;

    public VirtualLabProjectService(StemDbContext context, DiagramValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<VirtualLabProject> CreateProjectAsync(VirtualLabProjectRequest request, int? userId)
    {
        var (isValid, errors) = _validator.Validate(request.Diagram);
        if (!isValid)
        {
            throw new ArgumentException(string.Join("; ", errors));
        }

        var project = new VirtualLabProject
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Board = request.Board,
            Language = request.Language,
            CodeContent = request.Code,
            DiagramJson = request.Diagram.ValueKind == JsonValueKind.String 
                ? request.Diagram.GetString() 
                : request.Diagram.GetRawText(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.VirtualLabProjects.Add(project);
        await _context.SaveChangesAsync();

        return project;
    }

    public async Task<VirtualLabProject?> GetProjectAsync(Guid id)
    {
        return await _context.VirtualLabProjects.FindAsync(id);
    }

    public async Task<VirtualLabProject?> UpdateProjectAsync(Guid id, VirtualLabProjectRequest request)
    {
        var project = await _context.VirtualLabProjects.FindAsync(id);
        if (project == null) return null;

        var (isValid, errors) = _validator.Validate(request.Diagram);
        if (!isValid)
        {
            throw new ArgumentException(string.Join("; ", errors));
        }

        project.Name = request.Name;
        project.Board = request.Board;
        project.Language = request.Language;
        project.CodeContent = request.Code;
        project.DiagramJson = request.Diagram.ValueKind == JsonValueKind.String 
            ? request.Diagram.GetString() 
            : request.Diagram.GetRawText();
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return project;
    }
}
