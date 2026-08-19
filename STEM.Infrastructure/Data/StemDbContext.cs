using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MimeKit;
using STEM.Core.Entities;
using STEM.Core.Entities.Assessments;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Components;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Users;
using System.Text.Json;

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
    public DbSet<STEM.Core.Entities.Courses.File> Files => Set<STEM.Core.Entities.Courses.File>();

    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Schedule> Schedules => Set<Schedule>();

    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentQuizDetail> AssignmentQuizDetails => Set<AssignmentQuizDetail>();
    public DbSet<AssignmentReportDetail> AssignmentReportDetails => Set<AssignmentReportDetail>();
    public DbSet<AssignmentSimulationDetail> AssignmentSimulationDetails => Set<AssignmentSimulationDetail>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionFile> FileEntity => Set<SubmissionFile>();
    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<SubmissionComment> SubmissionComments => Set<SubmissionComment>();
    public DbSet<ResubmitRequest> ResubmitRequests => Set<ResubmitRequest>();

    public DbSet<VirtualLabProject> VirtualLabProjects => Set<VirtualLabProject>();
    public DbSet<Lab> Labs => Set<Lab>();
    public DbSet<LabClassAssignment> LabClassAssignments => Set<LabClassAssignment>();
    public DbSet<LabProgress> LabProgresses => Set<LabProgress>();
    public DbSet<ComponentGlueRegistry> ComponentGlueRegistry => Set<ComponentGlueRegistry>();
    public DbSet<AiQuotaUsage> AiQuotaUsages => Set<AiQuotaUsage>();

    public DbSet<ComponentDefinition> ComponentDefinitions => Set<ComponentDefinition>();
    public DbSet<ComponentSource> ComponentSources => Set<ComponentSource>();

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

        // Module -> Course
        modelBuilder.Entity<Module>()
            .HasOne(m => m.Course)
            .WithMany(c => c.Modules)
            .HasForeignKey(m => m.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Lesson -> Module
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
        modelBuilder.Entity<STEM.Core.Entities.Courses.File>()
            .ToTable("Files")
            .HasOne(f => f.Material)
            .WithMany()
            .HasForeignKey(f => f.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);

        // SubmissionFile -> FileEntity
        modelBuilder.Entity<SubmissionFile>()
            .ToTable("FileEntity");

        // Submission -> File
        modelBuilder.Entity<Submission>()
            .HasOne(s => s.File)
            .WithMany()
            .HasForeignKey(s => s.FileId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Submissions_Files_FileId");

        // Enrollment -> Class
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

        // AssignmentQuizDetail
        modelBuilder.Entity<AssignmentQuizDetail>()
            .HasOne(d => d.Assignment)
            .WithOne(a => a.QuizDetail)
            .HasForeignKey<AssignmentQuizDetail>(d => d.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssignmentQuizDetail>()
            .Property(d => d.QuestionsJson)
            .HasColumnType("jsonb");

        // AssignmentReportDetail
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

        // AssignmentSimulationDetail
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

        modelBuilder.Entity<Submission>()
            .Property(s => s.AutoGradeResultJson)
            .HasColumnType("jsonb")
            .HasConversion(new ValueConverter<string?, string?>(
                v => v ?? (string?)null,
                v => v ?? (string?)null));

        // SubmissionComment
        modelBuilder.Entity<SubmissionComment>()
            .Property(c => c.Body)
            .HasMaxLength(2000);

        modelBuilder.Entity<SubmissionComment>()
            .HasOne(c => c.Submission)
            .WithMany()
            .HasForeignKey(c => c.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SubmissionComment>()
            .HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // ResubmitRequest
        modelBuilder.Entity<ResubmitRequest>()
            .Property(r => r.Reason)
            .HasMaxLength(1000);

        modelBuilder.Entity<ResubmitRequest>()
            .Property(r => r.ReviewNote)
            .HasMaxLength(1000);

        modelBuilder.Entity<ResubmitRequest>()
            .HasOne(r => r.Assignment)
            .WithMany()
            .HasForeignKey(r => r.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ResubmitRequest>()
            .HasOne(r => r.Student)
            .WithMany()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResubmitRequest>()
            .HasOne(r => r.ReviewedBy)
            .WithMany()
            .HasForeignKey(r => r.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Metric -> Assignment
        modelBuilder.Entity<Metric>()
            .HasOne(m => m.Assignment)
            .WithMany(a => a.Metrics)
            .HasForeignKey(m => m.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Rubric -> Assignment
        modelBuilder.Entity<Rubric>()
            .HasOne(r => r.Assignment)
            .WithOne(a => a.Rubric)
            .HasForeignKey<Rubric>(r => r.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Notification -> User
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // AiQuotaUsage
        modelBuilder.Entity<AiQuotaUsage>()
            .HasIndex(usage => new { usage.UserId, usage.UsageDate })
            .IsUnique();

        // VirtualLabProject
        modelBuilder.Entity<VirtualLabProject>()
            .Property(project => project.Status)
            .HasMaxLength(20);

        modelBuilder.Entity<VirtualLabProject>()
            .Property(project => project.SimulationEventsJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<VirtualLabProject>()
            .HasIndex(project => project.LabId);

        modelBuilder.Entity<VirtualLabProject>()
            .HasOne<Lab>()
            .WithMany()
            .HasForeignKey(project => project.LabId)
            .OnDelete(DeleteBehavior.SetNull);

        // Lab
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

        // LabClassAssignment
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

        // LabProgress
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

        // ComponentGlueRegistry
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
                ComponentType = "wokwi-led",
                Label = "LED",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"pin","kind":"digital_output"}]}""",
                CreatedAt = componentSeedTime,
                UpdatedAt = componentSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-pushbutton",
                Label = "Push Button",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"pin","kind":"digital_input"}]}""",
                CreatedAt = componentSeedTime,
                UpdatedAt = componentSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-buzzer",
                Label = "Buzzer",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"pin","kind":"pwm_output"}]}""",
                CreatedAt = componentSeedTime,
                UpdatedAt = componentSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-potentiometer",
                Label = "Potentiometer",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"pin","kind":"analog_input"}]}""",
                CreatedAt = componentSeedTime,
                UpdatedAt = componentSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-servo",
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

        var robotKitSeedTime = new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<ComponentGlueRegistry>().HasData(
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-hc-sr04",
                Label = "HC-SR04 Ultrasonic Sensor",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"TRIG","kind":"digital_output"},{"name":"ECHO","kind":"digital_input"},{"name":"GND","kind":"ground"}]}""",
                CreatedAt = robotKitSeedTime,
                UpdatedAt = robotKitSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-l298n",
                Label = "L298N Motor Driver",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"IN1","kind":"digital_output"},{"name":"IN2","kind":"digital_output"},{"name":"IN3","kind":"digital_output"},{"name":"IN4","kind":"digital_output"},{"name":"ENA","kind":"pwm_output"},{"name":"ENB","kind":"pwm_output"},{"name":"OUT1","kind":"motor_output"},{"name":"OUT2","kind":"motor_output"},{"name":"OUT3","kind":"motor_output"},{"name":"OUT4","kind":"motor_output"},{"name":"VIN","kind":"power"},{"name":"GND","kind":"ground"},{"name":"5V","kind":"power"}]}""",
                CreatedAt = robotKitSeedTime,
                UpdatedAt = robotKitSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-dc-motor",
                Label = "DC Geared Motor / TT Motor",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"terminal1","kind":"motor_terminal"},{"name":"terminal2","kind":"motor_terminal"}]}""",
                CreatedAt = robotKitSeedTime,
                UpdatedAt = robotKitSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-battery-pack",
                Label = "Battery Pack 7.4V (2x18650)",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"+","kind":"power"},{"name":"-","kind":"ground"}]}""",
                CreatedAt = robotKitSeedTime,
                UpdatedAt = robotKitSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-power-switch",
                Label = "Power Switch",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"IN","kind":"power"},{"name":"OUT","kind":"power"}]}""",
                CreatedAt = robotKitSeedTime,
                UpdatedAt = robotKitSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-robot-wheel",
                Label = "Robot Wheel",
                Supported = true,
                PinRequirementsJson = """{"pins":[],"visualOnly":true}""",
                CreatedAt = robotKitSeedTime,
                UpdatedAt = robotKitSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-caster-wheel",
                Label = "Caster Wheel",
                Supported = true,
                PinRequirementsJson = """{"pins":[],"visualOnly":true}""",
                CreatedAt = robotKitSeedTime,
                UpdatedAt = robotKitSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-robot-chassis",
                Label = "Robot Chassis",
                Supported = true,
                PinRequirementsJson = """{"pins":[],"visualOnly":true}""",
                CreatedAt = robotKitSeedTime,
                UpdatedAt = robotKitSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-breadboard",
                Label = "Breadboard",
                Supported = true,
                PinRequirementsJson = """{"pins":[],"visualOnly":true}""",
                CreatedAt = robotKitSeedTime,
                UpdatedAt = robotKitSeedTime
            },
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-delivery-box",
                Label = "Mini Delivery Box",
                Supported = true,
                PinRequirementsJson = """{"pins":[],"visualOnly":true}""",
                CreatedAt = robotKitSeedTime,
                UpdatedAt = robotKitSeedTime
            });

        var nextTierSeedTime = new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<ComponentGlueRegistry>().HasData(
            new ComponentGlueRegistry
            {
                ComponentType = "wokwi-rgb-led",
                Label = "RGB LED",
                Supported = true,
                PinRequirementsJson = """{"pins":[{"name":"R","kind":"digital_output"},{"name":"G","kind":"digital_output"},{"name":"B","kind":"digital_output"},{"name":"COM","kind":"power_or_ground"}]}""",
                CreatedAt = nextTierSeedTime,
                UpdatedAt = nextTierSeedTime
            });

        var libSeedTime = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<ComponentGlueRegistry>().HasData(
            new ComponentGlueRegistry { ComponentType = "wokwi-flame-sensor", Label = "Flame Sensor", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"DOUT","kind":"digital_input"},{"name":"AOUT","kind":"analog"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-gas-sensor", Label = "MQ Gas Sensor", Supported = true, PinRequirementsJson = """{"pins":[{"name":"AOUT","kind":"analog"},{"name":"DOUT","kind":"digital_input"},{"name":"GND","kind":"ground"},{"name":"VCC","kind":"power"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-pir-motion-sensor", Label = "PIR Motion Sensor", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"OUT","kind":"digital_input"},{"name":"GND","kind":"ground"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-photoresistor-sensor", Label = "Light Sensor / LDR", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"DO","kind":"digital_input"},{"name":"AO","kind":"analog"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-ntc-temperature-sensor", Label = "Temperature Sensor (NTC)", Supported = true, PinRequirementsJson = """{"pins":[{"name":"GND","kind":"ground"},{"name":"VCC","kind":"power"},{"name":"OUT","kind":"analog"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-hx711", Label = "Load Cell HX711", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"DT","kind":"digital_input"},{"name":"SCK","kind":"digital_output"},{"name":"GND","kind":"ground"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-ir-receiver", Label = "IR Receiver", Supported = true, PinRequirementsJson = """{"pins":[{"name":"GND","kind":"ground"},{"name":"VCC","kind":"power"},{"name":"DAT","kind":"digital_input"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-membrane-keypad", Label = "Keypad (4x4)", Supported = true, PinRequirementsJson = """{"pins":[{"name":"R1","kind":"digital_output"},{"name":"R2","kind":"digital_output"},{"name":"R3","kind":"digital_output"},{"name":"R4","kind":"digital_output"},{"name":"C1","kind":"digital_input"},{"name":"C2","kind":"digital_input"},{"name":"C3","kind":"digital_input"},{"name":"C4","kind":"digital_input"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-ssd1306", Label = "OLED SSD1306", Supported = true, PinRequirementsJson = """{"pins":[{"name":"DATA","kind":"i2c"},{"name":"CLK","kind":"i2c"},{"name":"DC","kind":"digital_output"},{"name":"RST","kind":"digital_output"},{"name":"CS","kind":"digital_output"},{"name":"3V3","kind":"power"},{"name":"VIN","kind":"power"},{"name":"GND","kind":"ground"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-lcd1602", Label = "LCD 16x2", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VSS","kind":"ground"},{"name":"VDD","kind":"power"},{"name":"V0","kind":"analog"},{"name":"RS","kind":"digital_output"},{"name":"RW","kind":"digital_output"},{"name":"E","kind":"digital_output"},{"name":"D0","kind":"digital_output"},{"name":"D1","kind":"digital_output"},{"name":"D2","kind":"digital_output"},{"name":"D3","kind":"digital_output"},{"name":"D4","kind":"digital_output"},{"name":"D5","kind":"digital_output"},{"name":"D6","kind":"digital_output"},{"name":"D7","kind":"digital_output"},{"name":"A","kind":"power"},{"name":"K","kind":"ground"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-lcd1602-i2c", Label = "LCD 16x2 I2C", Supported = true, PinRequirementsJson = """{"pins":[{"name":"GND","kind":"ground"},{"name":"VCC","kind":"power"},{"name":"SDA","kind":"i2c"},{"name":"SCL","kind":"i2c"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-neopixel", Label = "NeoPixel / LED Strip", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VDD","kind":"power"},{"name":"DOUT","kind":"digital_output"},{"name":"VSS","kind":"ground"},{"name":"DIN","kind":"digital_input"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-led-bar-graph", Label = "LED Bar Graph", Supported = true, PinRequirementsJson = """{"pins":[{"name":"A1","kind":"digital_output"},{"name":"A2","kind":"digital_output"},{"name":"A3","kind":"digital_output"},{"name":"A4","kind":"digital_output"},{"name":"A5","kind":"digital_output"},{"name":"A6","kind":"digital_output"},{"name":"A7","kind":"digital_output"},{"name":"A8","kind":"digital_output"},{"name":"A9","kind":"digital_output"},{"name":"A10","kind":"digital_output"},{"name":"C1","kind":"ground"},{"name":"C2","kind":"ground"},{"name":"C3","kind":"ground"},{"name":"C4","kind":"ground"},{"name":"C5","kind":"ground"},{"name":"C6","kind":"ground"},{"name":"C7","kind":"ground"},{"name":"C8","kind":"ground"},{"name":"C9","kind":"ground"},{"name":"C10","kind":"ground"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-7segment", Label = "Seven Segment Display", Supported = true, PinRequirementsJson = """{"pins":[{"name":"COM.1","kind":"power_or_ground"},{"name":"COM.2","kind":"power_or_ground"},{"name":"A","kind":"digital_output"},{"name":"B","kind":"digital_output"},{"name":"C","kind":"digital_output"},{"name":"D","kind":"digital_output"},{"name":"E","kind":"digital_output"},{"name":"F","kind":"digital_output"},{"name":"G","kind":"digital_output"},{"name":"DP","kind":"digital_output"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-stepper-motor", Label = "Stepper Motor", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-ili9341", Label = "TFT Display (ILI9341)", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-dht11", Label = "DHT11", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"SDA","kind":"digital_bidirectional"},{"name":"NC","kind":"none"},{"name":"GND","kind":"ground"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-relay-module", Label = "Relay Module", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"IN","kind":"digital_input"},{"name":"GND","kind":"ground"},{"name":"NO","kind":"switch"},{"name":"COM","kind":"switch"},{"name":"NC","kind":"switch"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-fan", Label = "Fan / DC Fan", Supported = true, PinRequirementsJson = """{"pins":[{"name":"+","kind":"power"},{"name":"-","kind":"ground"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-water-pump", Label = "Water Pump / Mini Pump", Supported = true, PinRequirementsJson = """{"pins":[{"name":"+","kind":"power"},{"name":"-","kind":"ground"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-water-leak-sensor", Label = "Water Leak Sensor", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"S","kind":"digital_input"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-rain-sensor", Label = "Rain Sensor", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"DO","kind":"digital_input"},{"name":"AO","kind":"analog"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-soil-moisture-sensor", Label = "Soil Moisture Sensor", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"DO","kind":"digital_input"},{"name":"AO","kind":"analog"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-ir-obstacle-sensor", Label = "IR Obstacle Sensor", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"OUT","kind":"digital_input"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-line-tracking-sensor", Label = "Line Tracking Sensor", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"OUT","kind":"digital_input"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-color-sensor", Label = "Color Sensor", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"SDA","kind":"i2c"},{"name":"SCL","kind":"i2c"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-vibration-sensor", Label = "Vibration Sensor / SW-420", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"OUT","kind":"digital_input"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-solenoid-valve", Label = "Solenoid / Valve", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-esp32-cam", Label = "ESP32-CAM", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-wifi-cloud-node", Label = "WiFi / Cloud Node", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-dashboard-cloud", Label = "Dashboard / Cloud", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-robot-arm-base", Label = "Robot Arm Base", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-gripper", Label = "Gripper", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-conveyor-belt", Label = "Conveyor Belt", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-sorting-box", Label = "Sorting Box", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-ball", Label = "Ball", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-fire-extinguisher", Label = "Fire Extinguisher", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-water-tank", Label = "Water Tank", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-drone-frame", Label = "Drone Frame", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-propeller", Label = "Propeller", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-drone-motor", Label = "Drone Motor", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-stair-obstacle", Label = "Stair / Obstacle Block", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-trash-object", Label = "Trash Object", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-delivery-item", Label = "Delivery Package / Item", Supported = true, PinRequirementsJson = """{"pins":[],"visualOnly":true}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-mpu6050", Label = "IMU MPU6050", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"SCL","kind":"i2c"},{"name":"SDA","kind":"i2c"},{"name":"XDA","kind":"i2c"},{"name":"XCL","kind":"i2c"},{"name":"AD0","kind":"digital_input"},{"name":"INT","kind":"digital_output"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-esc", Label = "ESC (Electronic Speed Controller)", Supported = true, PinRequirementsJson = """{"pins":[{"name":"SIG","kind":"digital_input"},{"name":"GND","kind":"ground"},{"name":"BATT+","kind":"power"},{"name":"BATT-","kind":"ground"},{"name":"OUT+","kind":"power"},{"name":"OUT-","kind":"ground"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-heating-element", Label = "Heating Element", Supported = true, PinRequirementsJson = """{"pins":[{"name":"+","kind":"power"},{"name":"-","kind":"ground"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-ph-sensor", Label = "pH Sensor", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"PO","kind":"analog"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-line-tracking-3ch", Label = "Line Tracking Sensor (3 kênh)", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"OUT1","kind":"digital_input"},{"name":"OUT2","kind":"digital_input"},{"name":"OUT3","kind":"digital_input"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-line-tracking-5ch", Label = "Line Tracking Sensor (5 kênh)", Supported = true, PinRequirementsJson = """{"pins":[{"name":"VCC","kind":"power"},{"name":"GND","kind":"ground"},{"name":"OUT1","kind":"digital_input"},{"name":"OUT2","kind":"digital_input"},{"name":"OUT3","kind":"digital_input"},{"name":"OUT4","kind":"digital_input"},{"name":"OUT5","kind":"digital_input"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime },
            new ComponentGlueRegistry { ComponentType = "wokwi-lcd2004", Label = "LCD 20x4 I2C", Supported = true, PinRequirementsJson = """{"pins":[{"name":"GND","kind":"ground"},{"name":"VCC","kind":"power"},{"name":"SDA","kind":"i2c"},{"name":"SCL","kind":"i2c"}]}""", CreatedAt = libSeedTime, UpdatedAt = libSeedTime });

        // ComponentDefinition / ComponentSource — Multi-Provider Component
        // Registry (acquisition side). Deliberately separate tables from
        // ComponentGlueRegistry above (kept as-is, unrelated primary key
        // shape — string canonical type vs Guid identity here).
        modelBuilder.Entity<ComponentDefinition>()
            .HasIndex(component => component.CanonicalKey)
            .IsUnique();

        modelBuilder.Entity<ComponentDefinition>()
            .Property(component => component.CanonicalKey)
            .HasMaxLength(120);

        modelBuilder.Entity<ComponentDefinition>()
            .Property(component => component.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<ComponentDefinition>()
            .Property(component => component.Status)
            .HasMaxLength(30);

        modelBuilder.Entity<ComponentDefinition>()
            .Property(component => component.SimulationComponentType)
            .HasMaxLength(80);

        modelBuilder.Entity<ComponentDefinition>()
            .Property(component => component.PinsJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<ComponentSource>()
            .HasOne(source => source.Component)
            .WithMany(component => component.Sources)
            .HasForeignKey(source => source.ComponentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ComponentSource>()
            .HasIndex(source => new { source.Provider, source.ExternalId })
            .IsUnique();

        modelBuilder.Entity<ComponentSource>()
            .Property(source => source.Provider)
            .HasMaxLength(60);

        modelBuilder.Entity<ComponentSource>()
            .Property(source => source.ExternalId)
            .HasMaxLength(300);

        modelBuilder.Entity<ComponentSource>()
            .Property(source => source.LicenseStatus)
            .HasMaxLength(30);

        modelBuilder.Entity<ComponentSource>()
            .Property(source => source.AssetsJson)
            .HasColumnType("jsonb");
    }
}
