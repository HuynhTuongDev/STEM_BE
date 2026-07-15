using System;
using System.Threading.Tasks;
using STEM.Application.Dtos.VirtualLab;
using STEM.Core.Entities.Simulations;

namespace STEM.Application.Interfaces;

public interface IVirtualLabProjectService
{
    Task<VirtualLabProject> CreateProjectAsync(VirtualLabProjectRequest request, int? userId);
    Task<VirtualLabProject?> GetProjectAsync(Guid id);
    Task<VirtualLabProject?> UpdateProjectAsync(Guid id, VirtualLabProjectRequest request);
}
