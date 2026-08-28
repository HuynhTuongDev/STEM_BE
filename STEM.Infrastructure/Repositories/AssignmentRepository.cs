using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Assessments;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class AssignmentRepository : Repository<Assignment>, IAssignmentRepository
{
    public AssignmentRepository(StemDbContext context) : base(context)
    {
    }

    public override async Task<Assignment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Rubric)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Assignment>> GetByCourseIdAsync(
        int courseId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(assignment => assignment.Class)
                .ThenInclude(classEntity => classEntity!.Course)
            .Where(assignment => assignment.Class != null && assignment.Class.CourseId == courseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Assignment?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(assignment => assignment.Id == id)
            .Select(ProjectAssignmentProjection())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteDetailsAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        var quizDetails = await _context.AssignmentQuizDetails
            .Where(detail => detail.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);
        var reportDetails = await _context.AssignmentReportDetails
            .Where(detail => detail.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);
        var simulationDetails = await _context.AssignmentSimulationDetails
            .Where(detail => detail.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        _context.AssignmentQuizDetails.RemoveRange(quizDetails);
        _context.AssignmentReportDetails.RemoveRange(reportDetails);
        _context.AssignmentSimulationDetails.RemoveRange(simulationDetails);
    }

    public async Task<AssignmentQuizDetail?> GetQuizDetailAsync(
        int assignmentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AssignmentQuizDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.AssignmentId == assignmentId, cancellationToken);
    }

    public async Task<(IEnumerable<Assignment> Assignments, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? classId,
        int? courseId,
        int? schoolId,
        int? teacherId,
        int? studentId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .AsQueryable();

        // Include Enrollments, Submissions and Rubric
        if (studentId.HasValue)
        {
            query = query.Include(a => a.Class)
                .ThenInclude(c => c!.Enrollments)
                .Include(a => a.Submissions);
        }

        query = query.Include(a => a.Rubric);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(assignment => assignment.Title.ToLower().Contains(term));
        }

        if (classId.HasValue)
        {
            var classIdValue = classId.Value;
            query = query.Where(assignment => assignment.ClassId == classIdValue);
        }

        if (courseId.HasValue)
        {
            var courseIdValue = courseId.Value;
            query = query.Where(assignment => assignment.Class != null && assignment.Class.CourseId == courseIdValue);
        }

        if (schoolId.HasValue)
        {
            var schoolIdValue = schoolId.Value;
            query = query.Where(assignment => assignment.Class != null && assignment.Class.SchoolId == schoolIdValue);
        }

        if (teacherId.HasValue)
        {
            var teacherIdValue = teacherId.Value;
            query = query.Where(assignment => assignment.Class != null && assignment.Class.TeacherId == teacherIdValue);
        }

        if (studentId.HasValue)
        {
            var studentIdValue = studentId.Value;
            query = query.Where(assignment =>
                assignment.Class != null &&
                assignment.Class.Enrollments.Any(enrollment => enrollment.StudentId == studentIdValue));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var assignments = await query
            .OrderByDescending(assignment => assignment.CreatedAt)
            .ThenBy(assignment => assignment.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ProjectAssignmentProjection())
            .ToListAsync(cancellationToken);

        return (assignments, totalCount);
    }

    private static Expression<Func<Assignment, Assignment>> ProjectAssignmentProjection()
    {
        return assignment => new Assignment
        {
            Id = assignment.Id,
            ClassId = assignment.ClassId,
            Title = assignment.Title,
            CreatedAt = assignment.CreatedAt,
            UpdatedAt = assignment.UpdatedAt,
            Description = assignment.Description,
            AssignmentType = assignment.AssignmentType,
            DueDate = assignment.DueDate,
            MaxScore = assignment.MaxScore,
            RubricId = assignment.RubricId,
            AllowResubmit = assignment.AllowResubmit,
            ResubmitLimit = assignment.ResubmitLimit,
            Status = assignment.Status,
            CreatedById = assignment.CreatedById,
            Class = assignment.Class == null ? null : new Class
            {
                Id = assignment.Class.Id,
                ClassCode = assignment.Class.ClassCode,
                SchoolId = assignment.Class.SchoolId,
                CourseId = assignment.Class.CourseId,
                TeacherId = assignment.Class.TeacherId,
                StartDate = assignment.Class.StartDate,
                EndDate = assignment.Class.EndDate,
                CreatedAt = assignment.Class.CreatedAt,
                UpdatedAt = assignment.Class.UpdatedAt,
                Course = assignment.Class.Course == null ? null : new Course
                {
                    Id = assignment.Class.Course.Id,
                    Title = assignment.Class.Course.Title
                },
                School = assignment.Class.School == null ? null : new School
                {
                    Id = assignment.Class.School.Id,
                    Name = assignment.Class.School.Name
                },
                Teacher = assignment.Class.Teacher == null ? null : new User
                {
                    Id = assignment.Class.Teacher.Id,
                    FullName = assignment.Class.Teacher.FullName
                },
                Enrollments = assignment.Class.Enrollments
                    .Select(enrollment => new Enrollment
                    {
                        Id = enrollment.Id,
                        ClassId = enrollment.ClassId,
                        StudentId = enrollment.StudentId
                    })
                    .ToList()
            },
            Submissions = assignment.Submissions
                .Select(submission => new Submission
                {
                    Id = submission.Id,
                    AssignmentId = submission.AssignmentId,
                    StudentId = submission.StudentId,
                    AttemptNumber = submission.AttemptNumber,
                    FinalScore = submission.FinalScore,
                    Score = submission.Score,
                    AutoScore = submission.AutoScore,
                    Status = submission.Status
                })
                .ToList(),
            Metrics = assignment.Metrics
                .Select(metric => new Metric
                {
                    Id = metric.Id,
                    AssignmentId = metric.AssignmentId
                })
                .ToList(),
            QuizDetail = assignment.QuizDetail == null ? null : new AssignmentQuizDetail
            {
                Id = assignment.QuizDetail.Id,
                AssignmentId = assignment.QuizDetail.AssignmentId,
                QuestionsJson = assignment.QuizDetail.QuestionsJson,
                TimeLimitSeconds = assignment.QuizDetail.TimeLimitSeconds,
                ShuffleQuestions = assignment.QuizDetail.ShuffleQuestions,
                CreatedAt = assignment.QuizDetail.CreatedAt,
                UpdatedAt = assignment.QuizDetail.UpdatedAt
            },
            ReportDetail = assignment.ReportDetail == null ? null : new AssignmentReportDetail
            {
                Id = assignment.ReportDetail.Id,
                AssignmentId = assignment.ReportDetail.AssignmentId,
                Instructions = assignment.ReportDetail.Instructions,
                AllowedSubmissionTypesJson = assignment.ReportDetail.AllowedSubmissionTypesJson,
                AllowedFileExtensionsJson = assignment.ReportDetail.AllowedFileExtensionsJson,
                MaxFileSizeMb = assignment.ReportDetail.MaxFileSizeMb,
                CreatedAt = assignment.ReportDetail.CreatedAt,
                UpdatedAt = assignment.ReportDetail.UpdatedAt
            },
            SimulationDetail = assignment.SimulationDetail == null ? null : new AssignmentSimulationDetail
            {
                Id = assignment.SimulationDetail.Id,
                AssignmentId = assignment.SimulationDetail.AssignmentId,
                EnvironmentSource = assignment.SimulationDetail.EnvironmentSource,
                BaseDiagramJson = assignment.SimulationDetail.BaseDiagramJson,
                AllowedComponentTypesJson = assignment.SimulationDetail.AllowedComponentTypesJson,
                StudentInputMode = assignment.SimulationDetail.StudentInputMode,
                StarterCode = assignment.SimulationDetail.StarterCode,
                AnswerKeyJson = assignment.SimulationDetail.AnswerKeyJson,
                AutoGradingEnabled = assignment.SimulationDetail.AutoGradingEnabled,
                AutoGradingWeight = assignment.SimulationDetail.AutoGradingWeight,
                CreatedAt = assignment.SimulationDetail.CreatedAt,
                UpdatedAt = assignment.SimulationDetail.UpdatedAt
            },
            Rubric = assignment.Rubric == null ? null : new Rubric
            {
                Id = assignment.Rubric.Id,
                AssignmentId = assignment.Rubric.AssignmentId,
                Criteria = assignment.Rubric.Criteria,
                MaxScore = assignment.Rubric.MaxScore,
                CreatedAt = assignment.Rubric.CreatedAt,
                UpdatedAt = assignment.Rubric.UpdatedAt
            }
        };
    }
}
