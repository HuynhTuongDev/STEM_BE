using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Auth;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;
using STEM.Infrastructure.Repositories;
using STEM.Infrastructure.Services;
using STEM.Infrastructure.Services.Authentication;

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

        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddTransient<IEmailService, EmailService>();

        // Register Auth Handlers
        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();

        return services;
    }
}