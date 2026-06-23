using Microsoft.Extensions.DependencyInjection;
using STEM.Application.UseCases.Auth;
using STEM.Application.UseCases.LoginHistory;
using STEM.Application.UseCases.Notifications;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Schools;
using STEM.Application.UseCases.Courses;
using STEM.Application.UseCases.Classes;
using STEM.Application.UseCases.Users;
using STEM.Application.UseCases.Attendance;
using FluentValidation;
using STEM.Application.Validators;

namespace STEM.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth Handlers
        services.AddScoped<LoginHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<CreateUserBySchoolAdminHandler>();

        // School Handlers
        services.AddScoped<RegisterSchoolHandler>();
        services.AddScoped<UpdateSchoolHandler>();
        services.AddScoped<DeleteSchoolHandler>();

        // LoginHistory Handlers
        services.AddScoped<GetLoginHistoriesHandler>();

        // Notification Handlers
        services.AddScoped<NotificationHandler>();

        // Simulation Handlers
        services.AddScoped<SimulationHandler>();
        services.AddScoped<AiSuggestHandler>();
        services.AddHttpClient("Anthropic", client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com");
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        });

        // User Handlers
        services.AddScoped<GetUserProfileHandler>();
        services.AddScoped<UpdateUserProfileHandler>();
        services.AddScoped<UploadAvatarHandler>();
        services.AddScoped<GetUsersListHandler>();
        services.AddScoped<GetUserDetailHandler>();

        // Attendance Handlers
        services.AddScoped<CreateAttendanceHandler>();
        services.AddScoped<UpdateAttendanceHandler>();
        services.AddScoped<GetAttendanceHandler>();

        // Course Handlers
        services.AddScoped<GetCoursesListHandler>();
        services.AddScoped<GetCourseDetailHandler>();
        services.AddScoped<CreateCourseHandler>();
        services.AddScoped<UpdateCourseHandler>();
        services.AddScoped<DeleteCourseHandler>();

        // Class Handlers
        services.AddScoped<GetClassesListHandler>();
        services.AddScoped<GetClassDetailHandler>();
        services.AddScoped<CreateClassHandler>();
        services.AddScoped<UpdateClassHandler>();
        services.AddScoped<DeleteClassHandler>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<CreateUserBySchoolAdminValidator>();

        return services;
    }
}
