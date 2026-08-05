using Microsoft.Extensions.DependencyInjection;
using STEM.Application.UseCases.Auth;
using STEM.Application.UseCases.LoginHistory;
using STEM.Application.UseCases.Notifications;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Schools;
using STEM.Application.UseCases.Courses;
using STEM.Application.UseCases.Classes;
using STEM.Application.UseCases.Users;
using STEM.Application.UseCases.Teachers;
using STEM.Application.UseCases.Attendance;
using STEM.Application.UseCases.Assignments;
using STEM.Application.UseCases.Quizzes;
using STEM.Application.UseCases.Grading;
using STEM.Application.UseCases.VirtualLabs;
using STEM.Application.UseCases.Students;
using STEM.Application.UseCases.Schedules;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runners.Educational;
using STEM.Application.UseCases.Simulation.Runners.Mock;
using STEM.Application.UseCases.Simulation.Runners.Qemu;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Application.UseCases.Payments;
using FluentValidation;
using STEM.Application.Validators;
using STEM.Core.Repository;
using STEM.Application.Dtos.Auth;

namespace STEM.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth Handlers
        services.AddScoped<LoginHandler>();
        services.AddScoped<GoogleLoginHandler>();
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
        services.AddSingleton<VirtualLabDiagramService>();
        services.AddSingleton<EducationalProgramAnalyzer>();
        services.AddSingleton<EducationalEventGenerator>();
        services.AddSingleton<VirtualLabMockRunner>();
        services.AddSingleton<EducationalSimulationRunner>();
        services.AddSingleton<QemuEsp32Runner>();
        services.AddScoped<ISimulationRunnerResolver, SimulationRunnerResolver>();
        services.AddSingleton<IRunningSimulationRegistry, RunningSimulationRegistry>();
        services.AddSingleton<ICompileCoordinator, CompileCoordinator>();
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
        services.AddScoped<UpdateTeacherHandler>();
        services.AddScoped<DeleteTeacherHandler>();
        services.AddScoped<GetTeachersWithClassCountHandler>();

        // Attendance Handlers
        services.AddScoped<CreateAttendanceHandler>();
        services.AddScoped<UpdateAttendanceHandler>();
        services.AddScoped<GetAttendanceHandler>();

        // Assignment Handlers
        services.AddScoped<CreateAssignmentHandler>();
        services.AddScoped<GetAssignmentsHandler>();
        services.AddScoped<GetAssignmentDetailHandler>();
        services.AddScoped<UpdateAssignmentHandler>();
        services.AddScoped<DeleteAssignmentHandler>();

        // Quiz Handlers
        services.AddScoped<CreateQuizHandler>();
        services.AddScoped<GetQuizzesHandler>();
        services.AddScoped<GetQuizDetailHandler>();
        services.AddScoped<UpdateQuizHandler>();
        services.AddScoped<DeleteQuizHandler>();

        // Grading Handlers
        services.AddScoped<GetSubmissionsHandler>();
        services.AddScoped<GetSubmissionDetailHandler>();
        services.AddScoped<GradeSubmissionHandler>();
        services.AddScoped<UpdateSubmissionGradeHandler>();

        // Virtual Lab Handlers
        services.AddScoped<CreateVirtualLabHandler>();
        services.AddScoped<GetVirtualLabsHandler>();
        services.AddScoped<GetVirtualLabDetailHandler>();
        services.AddScoped<UpdateVirtualLabHandler>();
        services.AddScoped<DeleteVirtualLabHandler>();

        // Student Handlers
        services.AddScoped<GetStudentsHandler>();
        services.AddScoped<GetStudentLearningProgressHandler>();
        services.AddScoped<CreateStudentHandler>();
        services.AddScoped<UpdateStudentHandler>();
        services.AddScoped<DeleteStudentHandler>();

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
        services.AddScoped<AssignStudentsToClassHandler>();
        services.AddScoped<RemoveStudentFromClassHandler>();
        services.AddScoped<GetAvailableStudentsHandler>();

        // Schedule Handlers
        services.AddScoped<CreateScheduleHandler>();
        services.AddScoped<UpdateScheduleHandler>();
        services.AddScoped<DeleteScheduleHandler>();
        services.AddScoped<GetTeacherScheduleHandler>();
        services.AddScoped<GetStudentScheduleHandler>();

        // Payment Handlers
        services.AddScoped<GetPackagesHandler>();
        services.AddScoped<CreatePaymentHandler>();
        services.AddScoped<PaymentCallbackHandler>();
        services.AddScoped<GetPaymentsHandler>();
        services.AddScoped<GetTokenBalanceHandler>();
        services.AddScoped<GetTokenTransactionsHandler>();
        services.AddScoped<UseTokenHandler>();

        // Validators - register all validators from assembly
        services.AddValidatorsFromAssemblyContaining<CreateUserBySchoolAdminValidator>();

        // Override specific validator with DI support
        services.AddScoped<IValidator<CreateUserBySchoolAdminRequest>>(sp =>
            new CreateUserBySchoolAdminValidator(sp.GetRequiredService<IUserRepository>()));

        return services;
    }
}
