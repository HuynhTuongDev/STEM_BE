namespace STEM.Infrastructure.Data;

using STEM.Core.Entities;
using STEM.Core.Entities.Participants;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Users;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Quizzes;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Assessments;
using STEM.Core.Entities.Payments;
using STEM.Core.Entities.VirtualLabs;
using Microsoft.EntityFrameworkCore;

public class StemDbContext(DbContextOptions<StemDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<School> Schools => Set<School>();

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<File> Files => Set<File>();

    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<FileEntity> FileEntities => Set<FileEntity>();

    public DbSet<SimulationEntity> Simulations => Set<SimulationEntity>();
    public DbSet<SimulationTemplate> SimulationTemplates => Set<SimulationTemplate>();
    public DbSet<SimulationSession> SimulationSessions => Set<SimulationSession>();
    public DbSet<ExperimentLog> ExperimentLogs => Set<ExperimentLog>();
    public DbSet<LiveMonitoring> LiveMonitorings => Set<LiveMonitoring>();

    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();

    public DbSet<Rubric> Rubrics => Set<Rubric>();

    public DbSet<Notification> Notifications => Set<Notification>();

    // Payment
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentPackage> PaymentPackages => Set<PaymentPackage>();
    public DbSet<TokenAccount> TokenAccounts => Set<TokenAccount>();
    public DbSet<TokenTransaction> TokenTransactions => Set<TokenTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Master Administrator", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = 2, Name = "School Administrator", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = 3, Name = "Teacher", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = 4, Name = "Student", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        // User -> School
        modelBuilder.Entity<User>()
            .HasOne(u => u.School)
            .WithMany(s => s.Users)
            .HasForeignKey(u => u.SchoolId)
            .OnDelete(DeleteBehavior.SetNull);

        // User -> Role
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> RefreshTokens
        modelBuilder.Entity<RefreshToken>()
            .HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> LoginHistories
        modelBuilder.Entity<LoginHistory>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // School -> Classes
        modelBuilder.Entity<Class>()
            .HasOne(c => c.School)
            .WithMany(s => s.Classes)
            .HasForeignKey(c => c.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        // Class -> Course
        modelBuilder.Entity<Class>()
            .HasOne(c => c.Course)
            .WithMany(c => c.Classes)
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Class -> Teacher
        modelBuilder.Entity<Class>()
            .HasOne(c => c.Teacher)
            .WithMany()
            .HasForeignKey(c => c.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // Course -> School
        modelBuilder.Entity<Course>()
            .HasOne(c => c.School)
            .WithMany(s => s.Courses)
            .HasForeignKey(c => c.SchoolId)
            .OnDelete(DeleteBehavior.SetNull);

        // Quiz -> Class (thay vì Course)
        modelBuilder.Entity<Quiz>()
            .HasOne(q => q.Class)
            .WithMany(c => c.Quizzes)
            .HasForeignKey(q => q.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // SimulationEntity -> Class (thay vì Lesson)
        modelBuilder.Entity<SimulationEntity>()
            .HasOne(s => s.Class)
            .WithMany(c => c.VirtualLabs)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Module>()
            .HasOne(m => m.Course)
            .WithMany(c => c.Modules)
            .HasForeignKey(m => m.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Module -> Lessons
        modelBuilder.Entity<Lesson>()
            .HasOne(l => l.Module)
            .WithMany()
            .HasForeignKey(l => l.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Material -> Course
        modelBuilder.Entity<Material>()
            .HasOne(m => m.Course)
            .WithMany(c => c.Materials)
            .HasForeignKey(m => m.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // File -> Material
        modelBuilder.Entity<File>()
            .HasOne(f => f.Material)
            .WithMany()
            .HasForeignKey(f => f.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enrollment
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Class)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Attendance
        modelBuilder.Entity<AttendanceRecord>()
            .HasIndex(a => new { a.ClassId, a.StudentId, a.AttendanceDate })
            .IsUnique();

        modelBuilder.Entity<AttendanceRecord>()
            .Property(a => a.Status)
            .HasMaxLength(20);

        modelBuilder.Entity<AttendanceRecord>()
            .Property(a => a.Note)
            .HasMaxLength(500);

        modelBuilder.Entity<AttendanceRecord>()
            .HasOne(a => a.Class)
            .WithMany(c => c.AttendanceRecords)
            .HasForeignKey(a => a.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AttendanceRecord>()
            .HasOne(a => a.Student)
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AttendanceRecord>()
            .HasOne(a => a.MarkedBy)
            .WithMany()
            .HasForeignKey(a => a.MarkedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Announcement -> Class
        modelBuilder.Entity<Announcement>()
            .HasOne(a => a.Class)
            .WithMany(c => c.Announcements)
            .HasForeignKey(a => a.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Schedule -> Class
        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.Class)
            .WithMany(c => c.Schedules)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Room configuration
        modelBuilder.Entity<Room>()
            .HasIndex(r => r.RoomCode)
            .IsUnique();

        // Project -> Class
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Class)
            .WithMany()
            .HasForeignKey(p => p.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProjectMember
        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.Student)
            .WithMany()
            .HasForeignKey(pm => pm.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Assignment -> Class
        modelBuilder.Entity<Assignment>()
            .HasOne(a => a.Class)
            .WithMany()
            .HasForeignKey(a => a.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Submission -> Assignment
        modelBuilder.Entity<Submission>()
            .HasOne(s => s.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Submission>()
            .Property(s => s.Score)
            .HasColumnType("numeric(5,2)");

        modelBuilder.Entity<Submission>()
            .Property(s => s.Feedback)
            .HasMaxLength(1000);

        modelBuilder.Entity<Submission>()
            .HasOne(s => s.Student)
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Submission>()
            .HasOne(s => s.File)
            .WithMany()
            .HasForeignKey(s => s.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Submission>()
            .HasOne(s => s.GradedBy)
            .WithMany()
            .HasForeignKey(s => s.GradedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Metric -> Assignment
        modelBuilder.Entity<Metric>()
            .HasOne(m => m.Assignment)
            .WithMany(a => a.Metrics)
            .HasForeignKey(m => m.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // QuizQuestion -> Quiz
        modelBuilder.Entity<QuizQuestion>()
            .HasOne(q => q.Quiz)
            .WithMany(q => q.QuizQuestions)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        // QuizAnswer -> QuizQuestion
        modelBuilder.Entity<QuizAnswer>()
            .HasOne(a => a.Question)
            .WithMany(q => q.QuizAnswers)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Rubric -> Assignment
        modelBuilder.Entity<Rubric>()
            .HasOne(r => r.Assignment)
            .WithMany()
            .HasForeignKey(r => r.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Notification -> User
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // SimulationTemplate -> SimulationEntity
        modelBuilder.Entity<SimulationTemplate>()
            .HasOne(t => t.Simulation)
            .WithMany(s => s.SimulationTemplates)
            .HasForeignKey(t => t.SimulationId)
            .OnDelete(DeleteBehavior.Cascade);

        // SimulationSession -> Student
        modelBuilder.Entity<SimulationSession>()
            .HasOne(s => s.Student)
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // SimulationSession -> Template
        modelBuilder.Entity<SimulationSession>()
            .HasOne(s => s.Template)
            .WithMany(t => t.SimulationSessions)
            .HasForeignKey(s => s.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        // ExperimentLog -> Session
        modelBuilder.Entity<ExperimentLog>()
            .HasOne(e => e.Session)
            .WithMany(s => s.ExperimentLogs)
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // LiveMonitoring -> Session
        modelBuilder.Entity<LiveMonitoring>()
            .HasOne(l => l.Session)
            .WithMany(s => s.LiveMonitorings)
            .HasForeignKey(l => l.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // LiveMonitoring -> Teacher
        modelBuilder.Entity<LiveMonitoring>()
            .HasOne(l => l.Teacher)
            .WithMany()
            .HasForeignKey(l => l.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // Payment -> Buyer (User)
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Buyer)
            .WithMany(u => u.BuyerPayments)
            .HasForeignKey(p => p.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Payment -> Seller (User)
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Seller)
            .WithMany(u => u.SellerPayments)
            .HasForeignKey(p => p.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Payment -> Package
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Package)
            .WithMany(pkg => pkg.Payments)
            .HasForeignKey(p => p.PackageId)
            .OnDelete(DeleteBehavior.Restrict);

        // TokenAccount -> School
        modelBuilder.Entity<TokenAccount>()
            .HasOne(t => t.School)
            .WithMany(s => s.TokenAccounts)
            .HasForeignKey(t => t.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        // TokenTransaction -> School
        modelBuilder.Entity<TokenTransaction>()
            .HasOne(t => t.School)
            .WithMany(s => s.TokenTransactions)
            .HasForeignKey(t => t.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        // TokenTransaction -> Payment
        modelBuilder.Entity<TokenTransaction>()
            .HasOne(t => t.Payment)
            .WithMany()
            .HasForeignKey(t => t.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed Payment Packages
        modelBuilder.Entity<PaymentPackage>().HasData(
            new PaymentPackage 
            { 
                Id = 1, 
                Name = "1 Tháng", 
                Description = "Gói token sử dụng Virtual Lab trong 1 tháng", 
                DurationMonths = 1, 
                Price = 9.99m, 
                TokenAmount = 100, 
                IsActive = true, 
                IsFeatured = false, 
                SortOrder = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PaymentPackage 
            { 
                Id = 2, 
                Name = "3 Tháng", 
                Description = "Gói token sử dụng Virtual Lab trong 3 tháng - Tiết kiệm 10%", 
                DurationMonths = 3, 
                Price = 26.99m, 
                TokenAmount = 350, 
                IsActive = true, 
                IsFeatured = true, 
                SortOrder = 2,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PaymentPackage 
            { 
                Id = 3, 
                Name = "6 Tháng", 
                Description = "Gói token sử dụng Virtual Lab trong 6 tháng - Tiết kiệm 20%", 
                DurationMonths = 6, 
                Price = 49.99m, 
                TokenAmount = 800, 
                IsActive = true, 
                IsFeatured = true, 
                SortOrder = 3,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PaymentPackage 
            { 
                Id = 4, 
                Name = "9 Tháng", 
                Description = "Gói token sử dụng Virtual Lab trong 9 tháng - Tiết kiệm 25%", 
                DurationMonths = 9, 
                Price = 69.99m, 
                TokenAmount = 1350, 
                IsActive = true, 
                IsFeatured = false, 
                SortOrder = 4,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PaymentPackage 
            { 
                Id = 5, 
                Name = "1 Năm", 
                Description = "Gói token sử dụng Virtual Lab trong 12 tháng - Tiết kiệm 30%", 
                DurationMonths = 12, 
                Price = 89.99m, 
                TokenAmount = 2000, 
                IsActive = true, 
                IsFeatured = true, 
                SortOrder = 5,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

    }
}
