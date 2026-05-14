using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STEM.Application.Interfaces;
using STEM.Core.Repository;
using STEM.Infrastructure.Authentication;
using STEM.Infrastructure.Data;
using STEM.Infrastructure.Repositories;
using STEM.Infrastructure.Services;

namespace STEM.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<StemDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddTransient<IEmailService, EmailService>();

        return services;
    }
}