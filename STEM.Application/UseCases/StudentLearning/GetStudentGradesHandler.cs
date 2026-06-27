using STEM.Application.Dtos.StudentLearning;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.StudentLearning;

public class GetStudentGradesHandler
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IQuizAttemptRepository _quizAttemptRepository;
    private readonly IUserRepository _userRepository;

    public GetStudentGradesHandler(
        ISubmissionRepository submissionRepository,
        IQuizAttemptRepository quizAttemptRepository,
        IUserRepository userRepository)
    {
        _submissionRepository = submissionRepository;
        _quizAttemptRepository = quizAttemptRepository;
        _userRepository = userRepository;
    }

    public async Task<StudentGradesResponse> Handle(
        GetStudentGradesRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        StudentLearningGuard.EnsureStudent(currentUser);

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var submissions = await _submissionRepository.GetByStudentIdAsync(currentUserId, cancellationToken);
        var quizAttempts = await _quizAttemptRepository.GetByStudentIdAsync(currentUserId, cancellationToken);

        var assignmentGrades = submissions
            .Where(submission => submission.Score.HasValue)
            .Select(submission => new StudentGradeResponse
            {
                Type = "Assignment",
                SourceId = submission.Id,
                Title = submission.Assignment?.Title ?? string.Empty,
                Score = submission.Score ?? 0,
                MaxScore = 100,
                ClassId = submission.Assignment?.ClassId,
                ClassCode = submission.Assignment?.Class?.ClassCode,
                CourseId = submission.Assignment?.Class?.CourseId,
                CourseTitle = submission.Assignment?.Class?.Course?.Title,
                TeacherName = submission.Assignment?.Class?.Teacher?.FullName,
                Feedback = submission.Feedback,
                GradedAt = submission.GradedAt ?? submission.UpdatedAt
            });

        var quizGrades = quizAttempts
            .Select(attempt => new StudentGradeResponse
            {
                Type = "Quiz",
                SourceId = attempt.Id,
                Title = attempt.Quiz?.Title ?? string.Empty,
                Score = attempt.Score,
                MaxScore = 100,
                CourseId = attempt.Quiz?.CourseId,
                CourseTitle = attempt.Quiz?.Course?.Title,
                TeacherName = attempt.Quiz?.Course?.Teacher?.FullName,
                GradedAt = attempt.SubmittedAt
            });

        var grades = assignmentGrades.Concat(quizGrades);

        if (request.ClassId.HasValue)
        {
            grades = grades.Where(grade => grade.ClassId == request.ClassId.Value);
        }

        if (request.CourseId.HasValue)
        {
            grades = grades.Where(grade => grade.CourseId == request.CourseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = request.Type.Trim();
            grades = grades.Where(grade => grade.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        var orderedGrades = grades
            .OrderByDescending(grade => grade.GradedAt)
            .ThenByDescending(grade => grade.SourceId)
            .ToList();

        var totalCount = orderedGrades.Count;

        return new StudentGradesResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            AverageScore = totalCount == 0
                ? null
                : Math.Round(orderedGrades.Average(grade => grade.Score), 2, MidpointRounding.AwayFromZero),
            Items = orderedGrades
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList()
        };
    }
}
