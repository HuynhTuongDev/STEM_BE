namespace STEM.Infrastructure.Data;

using STEM.Core.Entities.Participants;
using STEM.Core.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Users;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Quizzes;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Schools;
using File = STEM.Core.Entities.Courses.File;
using Rubric = STEM.Core.Entities.Assessments.Rubric;

public class StemDbContext(DbContextOptions<StemDbContext> options) : DbContext(options)
{
    // Users & Security
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<LoginHistory> LoginHistories { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    // Schools
    public DbSet<School> Schools { get; set; } = null!;

    // Courses & Learning
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Module> Modules { get; set; } = null!;
    public DbSet<Lesson> Lessons { get; set; } = null!;
    public DbSet<Material> Materials { get; set; } = null!;
    public DbSet<File> Files { get; set; } = null!;

    // Classes
    public DbSet<Class> Classes { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<Announcement> Announcements { get; set; } = null!;
    public DbSet<Schedule> Schedules { get; set; } = null!;

    // Projects & Assignments
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<Assignment> Assignments { get; set; } = null!;
    public DbSet<Submission> Submissions { get; set; } = null!;
    public DbSet<Metric> Metrics { get; set; } = null!;
    public DbSet<ProjectMember> ProjectMembers { get; set; } = null!;

    // Quizzes & Assessment
    public DbSet<Quiz> Quizzes { get; set; } = null!;
    public DbSet<QuizQuestion> QuizQuestions { get; set; } = null!;
    public DbSet<QuizAnswer> QuizAnswers { get; set; } = null!;
    public DbSet<Rubric> Rubrics { get; set; } = null!;

    // Common
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<FileEntity> FileEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Master Administrator", CreatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc), UpdatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc) },
            new Role { Id = 2, Name = "School Administrator", CreatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc), UpdatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc) },
            new Role { Id = 3, Name = "Teacher", CreatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc), UpdatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc) },
            new Role { Id = 4, Name = "Student", CreatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc), UpdatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc) }
        );

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasOne(u => u.School)
                .WithMany(s => s.Users)
                .HasForeignKey(u => u.SchoolId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ProjectMember relationships
        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasOne(pm => pm.Student)
                .WithMany()
                .HasForeignKey(pm => pm.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Simulation relationships
        modelBuilder.Entity<SimulationEntity>(entity =>
        {
            entity.ToTable("Simulations");
        });

        modelBuilder.Entity<SimulationTemplate>(entity =>
        {
            entity.HasMany(t => t.SimulationSessions)
                .WithOne(s => s.Template)
                .HasForeignKey(s => s.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Certificates
        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
