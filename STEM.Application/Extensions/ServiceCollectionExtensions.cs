using Microsoft.Extensions.DependencyInjection;
using STEM.Application.UseCases.Auth;
using STEM.Application.UseCases.LoginHistory;
using STEM.Application.UseCases.Notifications;
using STEM.Application.UseCases.Users;
using FluentValidation;
using STEM.Application.Validators;

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
        services.AddScoped<ChangePasswordHandler>();

        // LoginHistory Handlers
        services.AddScoped<GetLoginHistoriesHandler>();

        // Notification Handlers
        services.AddScoped<NotificationHandler>();

        // User Handlers
        services.AddScoped<GetUserProfileHandler>();
        services.AddScoped<UpdateUserProfileHandler>();
        services.AddScoped<UploadAvatarHandler>();
        services.AddScoped<GetUsersListHandler>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        return services;
    }
}