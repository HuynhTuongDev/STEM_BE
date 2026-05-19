using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger for JWT Auth
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "STEM API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

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

app.Run();
