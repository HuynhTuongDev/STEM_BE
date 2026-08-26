using Microsoft.Extensions.DependencyInjection;
using STEM.Application.UseCases.Auth;
using STEM.Application.UseCases.LoginHistory;
using STEM.Application.UseCases.Notifications;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Schools;
using STEM.Application.UseCases.Courses;
using STEM.Application.UseCases.Syllabuses;
using STEM.Application.UseCases.SystemLogs;
using STEM.Application.UseCases.Classes;
using STEM.Application.UseCases.Users;
using STEM.Application.UseCases.Teachers;
using STEM.Application.UseCases.Attendance;
using STEM.Application.UseCases.Assignments;
using STEM.Application.UseCases.Grading;
using STEM.Application.UseCases.Students;
using STEM.Application.UseCases.Schedules;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runners.Educational;
using STEM.Application.UseCases.Simulation.Runners.Mock;
using STEM.Application.UseCases.Simulation.Runners.Qemu;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Application.UseCases.Curriculum;
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
        services.AddScoped<AiSuggestHandler>();
        services.AddScoped<LabAiAssistHandler>();
        services.AddSingleton<VirtualLabDiagramService>();
        services.AddSingleton<EducationalProgramAnalyzer>();
        services.AddSingleton<EducationalEventGenerator>();
        services.AddSingleton<VirtualLabMockRunner>();
        services.AddSingleton<EducationalSimulationRunner>();
        services.AddSingleton<QemuEsp32Runner>();
        services.AddScoped<ISimulationRunnerResolver, SimulationRunnerResolver>();
        services.AddSingleton<IRunningSimulationRegistry, RunningSimulationRegistry>();
        services.AddSingleton<ISimulationInputChannel, SimulationInputChannel>();
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
        services.AddScoped<UpdateAssignmentHandler>(sp => new UpdateAssignmentHandler(
            sp.GetRequiredService<IAssignmentRepository>(),
            sp.GetRequiredService<IClassRepository>(),
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<IRubricRepository>()
        ));
        services.AddScoped<DeleteAssignmentHandler>();
        services.AddScoped<SubmitQuizAssignmentHandler>();
        services.AddScoped<SubmitReportAssignmentHandler>();
        services.AddScoped<SubmitSimulationAssignmentHandler>();
        services.AddScoped<GetMySubmissionHandler>();
        services.AddScoped<GetMySubmissionsHandler>();


        // Grading Handlers
        services.AddScoped<GetSubmissionsHandler>();
        services.AddScoped<GetSubmissionDetailHandler>();
        services.AddScoped<GradeSubmissionHandler>();
        services.AddScoped<UpdateSubmissionGradeHandler>();
        services.AddScoped<GetSubmissionCommentsHandler>();
        services.AddScoped<CreateSubmissionCommentHandler>();
        services.AddScoped<UpdateSubmissionCommentHandler>();
        services.AddScoped<DeleteSubmissionCommentHandler>();
        services.AddScoped<CreateResubmitRequestHandler>();
        services.AddScoped<GetResubmitRequestsHandler>();
        services.AddScoped<ApproveResubmitRequestHandler>();
        services.AddScoped<RejectResubmitRequestHandler>();

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

        // Syllabus Handlers (from Syllabuses folder)
        services.AddScoped<GetSyllabusesListHandler>();
        services.AddScoped<GetSyllabusDetailHandler>();
        services.AddScoped<GetSyllabusStructureHandler>();
        services.AddScoped<GetGradeLevelsListHandler>();
        services.AddScoped<GetSystemLogsListHandler>();

        // Curriculum Handlers (from Curriculum folder - fully qualified to avoid ambiguity)
        services.AddScoped<UseCases.Curriculum.GetSyllabiHandler>();
        services.AddScoped<UseCases.Curriculum.GetSyllabusByIdHandler>();
        services.AddScoped<UseCases.Curriculum.CreateSyllabusHandler>();
        services.AddScoped<UseCases.Curriculum.UpdateSyllabusHandler>();
        services.AddScoped<UseCases.Curriculum.DeleteSyllabusHandler>();
        services.AddScoped<UseCases.Curriculum.PublishSyllabusHandler>();
        services.AddScoped<UseCases.Curriculum.ArchiveSyllabusHandler>();
        services.AddScoped<UseCases.Curriculum.UnpublishSyllabusHandler>();
        services.AddScoped<UseCases.Curriculum.RestoreSyllabusHandler>();
        services.AddScoped<UseCases.Curriculum.GetGradeLevelsHandler>();
        services.AddScoped<UseCases.Curriculum.GetGradeLevelByIdHandler>();
        services.AddScoped<UseCases.Curriculum.CreateGradeLevelHandler>();
        services.AddScoped<UseCases.Curriculum.UpdateGradeLevelHandler>();
        services.AddScoped<UseCases.Curriculum.DeleteGradeLevelHandler>();

        // Class Handlers
        services.AddScoped<GetClassesListHandler>();
        services.AddScoped<GetClassDetailHandler>();
        services.AddScoped<CreateClassHandler>();
        services.AddScoped<UpdateClassHandler>();
        services.AddScoped<DeleteClassHandler>();
        services.AddScoped<AssignStudentsToClassHandler>();
        services.AddScoped<RemoveStudentFromClassHandler>();
        services.AddScoped<GetAvailableStudentsHandler>();
        services.AddScoped<GetStudentTemplateHandler>();
        services.AddScoped<ImportStudentsHandler>();

        // Schedule Handlers
        services.AddScoped<CreateScheduleHandler>();
        services.AddScoped<UpdateScheduleHandler>();
        services.AddScoped<DeleteScheduleHandler>();
        services.AddScoped<GetTeacherScheduleHandler>();
        services.AddScoped<GetStudentScheduleHandler>();

        // Module Handlers
        services.AddScoped<GetModulesHandler>();
        services.AddScoped<GetModuleByIdHandler>();
        services.AddScoped<CreateModuleHandler>();
        services.AddScoped<UpdateModuleHandler>();
        services.AddScoped<DeleteModuleHandler>();
        services.AddScoped<ReorderModulesHandler>();
        services.AddScoped<GetModulesByClassHandler>();
        
        // Lesson Handlers
        services.AddScoped<GetLessonsHandler>();
        services.AddScoped<GetLessonByIdHandler>();
        services.AddScoped<CreateLessonHandler>();
        services.AddScoped<UpdateLessonHandler>();
        services.AddScoped<DeleteLessonHandler>();
        services.AddScoped<ReorderLessonsHandler>();

        // Validators - register all validators from assembly
        services.AddValidatorsFromAssemblyContaining<CreateUserBySchoolAdminValidator>();

        // Override specific validator with DI support
        services.AddScoped<IValidator<CreateUserBySchoolAdminRequest>>(sp =>
            new CreateUserBySchoolAdminValidator(sp.GetRequiredService<IUserRepository>()));

        return services;
    }
}
