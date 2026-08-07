using Microsoft.Extensions.DependencyInjection;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runners.Educational;
using STEM.Application.UseCases.Simulation.Runners.Mock;
using STEM.Application.UseCases.Simulation.Runners.Qemu;

namespace STEM.Application.UseCases.Simulation.Runtime;

public sealed class SimulationRunnerResolver : ISimulationRunnerResolver
{
    private readonly IServiceProvider _serviceProvider;

    public SimulationRunnerResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ISimulationRunner Resolve(string mode) => mode.Trim().ToLowerInvariant() switch
    {
        "mock" => _serviceProvider.GetRequiredService<VirtualLabMockRunner>(),
        "educational" => _serviceProvider.GetRequiredService<EducationalSimulationRunner>(),
        "qemu" => _serviceProvider.GetRequiredService<QemuEsp32Runner>(),
        _ => _serviceProvider.GetRequiredService<VirtualLabMockRunner>()
    };
}
