using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STEM.Application.Interfaces;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;
using STEM.Infrastructure.Repositories;
using STEM.Infrastructure.Services;
using STEM.Infrastructure.Services.Authentication;
using STEM.Infrastructure.Services.Wokwi;
using Supabase;

namespace STEM.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<StemDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddHttpContextAccessor();

        // Specific Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ISimulationRepository, SimulationRepository>();
        services.AddScoped<ISimulationSessionRepository, SimulationSessionRepository>();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IWokwiService, WokwiService>();
        services.AddTransient<IEmailService, EmailService>();

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

        services.AddScoped<IFileService, SupabaseStorageService>();

        return services;
    }
}
