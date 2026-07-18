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
    public DbSet<AssignmentQuizDetail> AssignmentQuizDetails => Set<AssignmentQuizDetail>();
    public DbSet<AssignmentReportDetail> AssignmentReportDetails => Set<AssignmentReportDetail>();
    public DbSet<AssignmentSimulationDetail> AssignmentSimulationDetails => Set<AssignmentSimulationDetail>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<FileEntity> FileEntities => Set<FileEntity>();

    public DbSet<SimulationEntity> Simulations => Set<SimulationEntity>();
    public DbSet<SimulationTemplate> SimulationTemplates => Set<SimulationTemplate>();
    public DbSet<SimulationSession> SimulationSessions => Set<SimulationSession>();
    public DbSet<ExperimentLog> ExperimentLogs => Set<ExperimentLog>();
    public DbSet<LiveMonitoring> LiveMonitorings => Set<LiveMonitoring>();
    
    public DbSet<VirtualLabProject> VirtualLabProjects => Set<VirtualLabProject>();
    public DbSet<Lab> Labs => Set<Lab>();
    public DbSet<LabClassAssignment> LabClassAssignments => Set<LabClassAssignment>();
    public DbSet<LabProgress> LabProgresses => Set<LabProgress>();
    public DbSet<ComponentGlueRegistry> ComponentGlueRegistry => Set<ComponentGlueRegistry>();

    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();

    public DbSet<Rubric> Rubrics => Set<Rubric>();

    public DbSet<Notification> Notifications => Set<Notification>();

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

        modelBuilder.Entity<Assignment>()
            .Property(a => a.AssignmentType)
            .HasMaxLength(40);

        modelBuilder.Entity<Assignment>()
            .Property(a => a.Status)
            .HasMaxLength(20);

        modelBuilder.Entity<Assignment>()
            .Property(a => a.MaxScore)
            .HasColumnType("numeric(6,2)");

        modelBuilder.Entity<AssignmentQuizDetail>()
            .HasOne(d => d.Assignment)
            .WithOne(a => a.QuizDetail)
            .HasForeignKey<AssignmentQuizDetail>(d => d.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssignmentQuizDetail>()
            .Property(d => d.QuestionsJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<AssignmentReportDetail>()
            .HasOne(d => d.Assignment)
            .WithOne(a => a.ReportDetail)
            .HasForeignKey<AssignmentReportDetail>(d => d.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssignmentReportDetail>()
            .Property(d => d.AllowedSubmissionTypesJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<AssignmentReportDetail>()
            .Property(d => d.AllowedFileExtensionsJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<AssignmentSimulationDetail>()
            .HasOne(d => d.Assignment)
            .WithOne(a => a.SimulationDetail)
            .HasForeignKey<AssignmentSimulationDetail>(d => d.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssignmentSimulationDetail>()
            .Property(d => d.BaseDiagramJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<AssignmentSimulationDetail>()
            .Property(d => d.AllowedComponentTypesJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<AssignmentSimulationDetail>()
            .Property(d => d.AnswerKeyJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<AssignmentSimulationDetail>()
            .Property(d => d.EnvironmentSource)
            .HasMaxLength(40);

        modelBuilder.Entity<AssignmentSimulationDetail>()
            .Property(d => d.StudentInputMode)
            .HasMaxLength(40);

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
            .Property(s => s.AutoScore)
            .HasColumnType("numeric(5,2)");

        modelBuilder.Entity<Submission>()
            .Property(s => s.FinalScore)
            .HasColumnType("numeric(5,2)");

        modelBuilder.Entity<Submission>()
            .Property(s => s.Status)
            .HasMaxLength(20);

        modelBuilder.Entity<Submission>()
            .Property(s => s.ContentJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Submission>()
            .Property(s => s.AutoGradeResultJson)
            .HasColumnType("jsonb");

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

        modelBuilder.Entity<Lab>()
            .HasOne(l => l.CreatedBy)
            .WithMany()
            .HasForeignKey(l => l.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Lab>()
            .HasOne(l => l.LinkedAssignment)
            .WithMany()
            .HasForeignKey(l => l.LinkedAssignmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Lab>()
            .Property(l => l.Title)
            .HasMaxLength(200);

        modelBuilder.Entity<Lab>()
            .Property(l => l.Category)
            .HasMaxLength(40);

        modelBuilder.Entity<Lab>()
            .Property(l => l.ThumbnailUrl)
            .HasMaxLength(2048);

        modelBuilder.Entity<Lab>()
            .Property(l => l.SimulationMode)
            .HasMaxLength(40);

        modelBuilder.Entity<Lab>()
            .Property(l => l.BoardType)
            .HasMaxLength(40);

        modelBuilder.Entity<Lab>()
            .Property(l => l.CircuitConfigJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Lab>()
            .Property(l => l.AllowedComponentTypesJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Lab>()
            .Property(l => l.WokwiProjectId)
            .HasMaxLength(80);

        modelBuilder.Entity<Lab>()
            .Property(l => l.WokwiProjectUrl)
            .HasMaxLength(2048);

        modelBuilder.Entity<Lab>()
            .Property(l => l.Status)
            .HasMaxLength(20);

        modelBuilder.Entity<Lab>()
            .HasIndex(l => l.Status);

        modelBuilder.Entity<Lab>()
            .HasIndex(l => l.Category);

        modelBuilder.Entity<LabClassAssignment>()
            .HasOne(assignment => assignment.Lab)
            .WithMany(lab => lab.ClassAssignments)
            .HasForeignKey(assignment => assignment.LabId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LabClassAssignment>()
            .HasOne(assignment => assignment.Class)
            .WithMany()
            .HasForeignKey(assignment => assignment.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LabClassAssignment>()
            .HasIndex(assignment => new { assignment.LabId, assignment.ClassId })
            .IsUnique();

        modelBuilder.Entity<LabProgress>()
            .HasOne(progress => progress.Lab)
            .WithMany(lab => lab.Progresses)
            .HasForeignKey(progress => progress.LabId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LabProgress>()
            .HasOne(progress => progress.Student)
            .WithMany()
            .HasForeignKey(progress => progress.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LabProgress>()
            .HasIndex(progress => new { progress.LabId, progress.StudentId })
            .IsUnique();

        modelBuilder.Entity<ComponentGlueRegistry>()
            .HasKey(component => component.ComponentType);

        modelBuilder.Entity<ComponentGlueRegistry>()
            .Property(component => component.ComponentType)
            .HasMaxLength(80);

        modelBuilder.Entity<ComponentGlueRegistry>()
            .Property(component => component.Label)
            .HasMaxLength(120);

        modelBuilder.Entity<ComponentGlueRegistry>()
            .Property(component => component.PinRequirementsJson)
            .HasColumnType("jsonb");

        var componentSeedTime = new DateTime(2026, 7, 9, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<ComponentGlueRegistry>().HasData(
            new ComponentGlueRegistry
            {
                ComponentType = "led",
                Label = "LED",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"pin","kind":"digital_output"}]}""",
                CreatedAt = componentSeedTime,
                UpdatedAt = componentSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "push_button",
                Label = "Push Button",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"pin","kind":"digital_input"}]}""",
                CreatedAt = componentSeedTime,
                UpdatedAt = componentSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "buzzer",
                Label = "Buzzer",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"pin","kind":"pwm_output"}]}""",
                CreatedAt = componentSeedTime,
                UpdatedAt = componentSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "potentiometer",
                Label = "Potentiometer",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"pin","kind":"analog_input"}]}""",
                CreatedAt = componentSeedTime,
                UpdatedAt = componentSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "servo",
                Label = "Servo",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"pin","kind":"pwm_output"}]}""",
                CreatedAt = componentSeedTime,
                UpdatedAt = componentSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "dht22",
                Label = "DHT22",
                Supported = false,
                PinRequirementsJson = """{"pins":[{"name":"data","kind":"digital_bidirectional"}]}""",
                CreatedAt = componentSeedTime,
                UpdatedAt = componentSeedTime
            });
    }
}
