using Microsoft.Extensions.DependencyInjection;
using STEM.Application.UseCases.Auth;
using STEM.Application.UseCases.LoginHistory;

namespace STEM.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth Handlers
        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<ResetPasswordHandler>();

        // LoginHistory Handlers
        services.AddScoped<GetLoginHistoriesHandler>();

        return services;
    }
}