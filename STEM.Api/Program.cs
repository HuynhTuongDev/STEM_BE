using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using STEM.Api.Hubs;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Infrastructure.Extensions;
using STEM.Application.Extensions;
using STEM.Core.Entities.Users;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<STEM.Api.Filters.ValidationExceptionFilter>();
});
builder.Services.AddSignalR();
builder.Services.AddSingleton<ISimulationEventBroadcaster, SignalRSimulationEventBroadcaster>();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger for JWT Auth
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "STEM API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token",
        In = ParameterLocation.Header,
        Name = "Authorization"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];

if (!string.IsNullOrEmpty(secretKey))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/virtual-lab"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });
}

// Add RBAC Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Master Administrator: System/Developer operations only
    options.AddPolicy("MasterOnly", policy =>
        policy.RequireRole(RoleNames.MasterAdministrator));

    // School Administrator: Business operations (students, classes, grades, etc.)
    options.AddPolicy("SchoolAdminOnly", policy =>
        policy.RequireRole(RoleNames.SchoolAdministrator));

    // Teachers & School Admins: Course/Class management
    options.AddPolicy("TeacherAndAbove", policy =>
        policy.RequireRole(RoleNames.SchoolAdministrator, RoleNames.Teacher));

    // All authenticated roles: Students, Teachers, School Admins
    options.AddPolicy("StudentAndAbove", policy =>
        policy.RequireRole(RoleNames.SchoolAdministrator, RoleNames.Teacher, RoleNames.Student));

    // Legacy: Both admin types (avoid using, prefer specific policies above)
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(RoleNames.MasterAdministrator, RoleNames.SchoolAdministrator));
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Use CORS before Authentication/Authorization
app.UseCors("AllowFrontend");

// Use Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Redirect root to Swagger UI
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger/index.html", permanent: false);
    return Task.CompletedTask;
});

app.MapControllers();
app.MapHub<VirtualLabHub>("/hubs/virtual-lab");

app.Run();
