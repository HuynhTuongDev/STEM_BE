using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Core.Interfaces;
using STEM.Core.Repository;
using STEM.Infrastructure.Repositories;
using STEM.Infrastructure.Services;
using STEM.Infrastructure.Services.Authentication;
using STEM.Infrastructure.Services.Payments;
using STEM.Infrastructure.Services.Simulation;
using STEM.Infrastructure.Services.Wokwi;
using Supabase;

namespace STEM.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<StemDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null)));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddHttpContextAccessor();

        // Specific Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ISyllabusRepository, SyllabusRepository>();
        services.AddScoped<ISystemLogRepository, SystemLogRepository>();
        services.AddScoped<ISystemLogService, SystemLogService>();
        services.AddScoped<IClassRepository, ClassRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddScoped<IRubricRepository, RubricRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<ISubmissionCommentRepository, SubmissionCommentRepository>();
        services.AddScoped<IResubmitRequestRepository, ResubmitRequestRepository>();
        services.AddScoped<ISchoolRepository, SchoolRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IWokwiService, WokwiService>();
        services.AddTransient<IEmailService, EmailService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Curriculum Repositories
        services.AddScoped<ISyllabusRepository, SyllabusRepository>();
        services.AddScoped<IGradeLevelRepository, GradeLevelRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();

        services.AddScoped<IVirtualLabProjectService, VirtualLabProjectService>();
        services.AddHttpClient<ILabService, LabService>();
        services.AddHttpClient<ILabAiProvider, BeeknoeeLabAiProvider>(client =>
        {
            var baseUrl = configuration["Beeknoee:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = "https://platform.beeknoee.com/api/v1";
            if (!baseUrl.EndsWith('/')) baseUrl += "/";
            client.BaseAddress = new Uri(baseUrl);
        });
        services.AddScoped<ISimulationCompileService, SimulationCompileService>();
        services.AddScoped<IVirtualLabRuntimeService, VirtualLabRuntimeService>();
        services.AddScoped<ISimulationEventStore, SimulationEventStore>();
        services.AddScoped<IFirmwareCacheService, FirmwareCacheService>();
        services.AddSingleton<IPrecompileTriggerService, PrecompileTriggerService>();
        services.AddHostedService<TokenExpirationBackgroundService>();
        services.AddHostedService<NotificationBackgroundService>();
        services.AddHostedService<ProgressAlertBackgroundService>();
        services.AddHostedService<SchoolAdminNotificationBackgroundService>();
        services.AddScoped<IAiQuotaUsageStore, AiQuotaUsageStore>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // AI Quota Handlers
        services.AddScoped<STEM.Application.UseCases.Simulation.GetUserAiQuotaHandler>();
        services.AddScoped<STEM.Application.UseCases.Simulation.LabAiAssistHandler>();

        // Supabase Configuration
        var supabaseUrl = configuration["Supabase:Url"];
        var supabaseKey = configuration["Supabase:Key"];
        
        if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey))
        {
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };
            services.AddScoped<Supabase.Client>(_ => new Supabase.Client(supabaseUrl, supabaseKey, options));
        }

        services.AddScoped<IFileRepository, FileEntityRepository>();
        services.AddScoped<IFileService, SupabaseStorageService>();

        // Payment Repositories
        services.AddScoped<IPaymentPackageRepository, PaymentPackageRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITokenAccountRepository, TokenAccountRepository>();
        services.AddScoped<ITokenTransactionRepository, TokenTransactionRepository>();
        services.AddScoped<ITokenAllocationRepository, TokenAllocationRepository>();

        // PayOS Service
        services.AddScoped<IPayOSService, PayOSService>();

        // Payment Handlers
        services.AddScoped<STEM.Application.UseCases.Payments.GetPackagesHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.CreatePaymentHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.GetBalanceHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.GetPaymentsHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.GetAllocationsHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.GetTransactionsHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.AllocateTokensHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.RevokeAllocationHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.PaymentWebhookHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.GetUsersWithTokensHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.CreatePackageHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.UpdatePackageHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.DeletePackageHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.RevokeExpiredAllocationsHandler>();
        services.AddScoped<STEM.Application.UseCases.Payments.BulkAllocateTokensByRoleHandler>();

        // Multi-Provider Component Architecture (acquisition side only —
        // never referenced by VirtualLabRuntimeService/ISimulationRunnerResolver).
        services.Configure<STEM.Infrastructure.VirtualLab.Components.Providers.Fritzing.FritzingOptions>(
            configuration.GetSection(STEM.Infrastructure.VirtualLab.Components.Providers.Fritzing.FritzingOptions.SectionName));
        services.AddHttpClient(STEM.Application.UseCases.Components.Providers.Fritzing.FritzingConstants.ProviderName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("StemFlow-ComponentImporter/1.0");
        });
        services.AddSingleton<STEM.Application.UseCases.Components.Abstractions.IComponentProvider,
            STEM.Infrastructure.VirtualLab.Components.Providers.Fritzing.FritzingComponentProvider>();

        services.Configure<STEM.Infrastructure.VirtualLab.Components.Providers.KiCad.KiCadOptions>(
            configuration.GetSection(STEM.Infrastructure.VirtualLab.Components.Providers.KiCad.KiCadOptions.SectionName));
        services.AddHttpClient(STEM.Application.UseCases.Components.Providers.KiCad.KiCadConstants.ProviderName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("StemFlow-ComponentImporter/1.0");
        });
        services.AddSingleton<STEM.Application.UseCases.Components.Abstractions.IComponentProvider,
            STEM.Infrastructure.VirtualLab.Components.Providers.KiCad.KiCadComponentProvider>();

        services.AddSingleton<STEM.Application.UseCases.Components.Abstractions.IComponentProviderAggregator,
            STEM.Application.UseCases.Components.ComponentProviderAggregator>();
        services.AddSingleton<STEM.Application.UseCases.Components.Abstractions.IComponentNormalizer,
            STEM.Application.UseCases.Components.ComponentNormalizer>();
        services.AddScoped<STEM.Application.UseCases.Components.Abstractions.IComponentRegistry,
            STEM.Infrastructure.VirtualLab.Components.ComponentRegistryService>();

        return services;
    }
}
