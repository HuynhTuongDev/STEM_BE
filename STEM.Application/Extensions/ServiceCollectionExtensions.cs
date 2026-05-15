using Microsoft.Extensions.DependencyInjection;
using STEM.Application.UseCases.Auth;
using STEM.Application.UseCases.LoginHistory;
using STEM.Application.UseCases.Notifications;

namespace STEM.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth Handlers
        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();

        // LoginHistory Handlers
        services.AddScoped<GetLoginHistoriesHandler>();

        // Notification Handlers
        services.AddScoped<NotificationHandler>();

        return services;
    }
}