using Microsoft.Extensions.DependencyInjection;
using STEM.Application.UseCases.Auth;

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

        // TODO: Add Order, Payment, and Product handlers when implemented

        return services;
    }
}