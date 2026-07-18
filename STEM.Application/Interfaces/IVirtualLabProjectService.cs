using System;
using System.Threading.Tasks;
using STEM.Application.Dtos.VirtualLab;
using STEM.Core.Entities.Simulations;

namespace STEM.Application.Interfaces;

public interface IVirtualLabProjectService
{
    Task<VirtualLabProject> CreateProjectAsync(VirtualLabProjectRequest request, int userId);

    /// <summary>Returns null if not found. Throws UnauthorizedAccessException if the project exists but is owned by a different user.</summary>
    Task<VirtualLabProject?> GetProjectAsync(Guid id, int currentUserId);

    /// <summary>Returns null if not found. Throws UnauthorizedAccessException if the project exists but is owned by a different user.</summary>
    Task<VirtualLabProject?> UpdateProjectAsync(Guid id, VirtualLabProjectRequest request, int currentUserId);
}
