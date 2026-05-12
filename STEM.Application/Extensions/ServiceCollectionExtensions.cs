using Microsoft.Extensions.DependencyInjection;
using STEM.Application.Interfaces;
using STEM.Application.Services;

namespace STEM.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}