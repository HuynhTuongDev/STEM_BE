using STEM.Application.Dtos.StudentLearning;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Quizzes;

namespace STEM.Application.UseCases.StudentLearning;

internal static class StudentLearningMapper
{
    public static StudentAssignmentResponse ToAssignmentResponse(Assignment assignment, int studentId)
    {
        var submission = assignment.Submissions
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault();

        return new StudentAssignmentResponse
        {
            Id = assignment.Id,
            Title = assignment.Title,
            ClassId = assignment.ClassId,
            ClassCode = assignment.Class?.ClassCode ?? string.Empty,
            CourseId = assignment.Class?.CourseId ?? 0,
            CourseTitle = assignment.Class?.Course?.Title ?? string.Empty,
            TeacherId = assignment.Class?.TeacherId ?? 0,
            TeacherName = assignment.Class?.Teacher?.FullName ?? string.Empty,
            SubmissionStatus = GetSubmissionStatus(submission),
            SubmissionId = submission?.Id,
            SubmissionFileUrl = submission?.File?.Url,
            SubmittedAt = submission?.CreatedAt,
            Score = submission?.Score,
            CreatedAt = assignment.CreatedAt,
            UpdatedAt = assignment.UpdatedAt
        };
    }

    public static SubmissionStatusResponse ToSubmissionStatus(Assignment assignment, Submission? submission)
    {
        return new SubmissionStatusResponse
        {
            AssignmentId = assignment.Id,
            AssignmentTitle = assignment.Title,
            IsSubmitted = submission != null,
            Status = GetSubmissionStatus(submission),
            SubmissionId = submission?.Id,
            FileId = submission?.FileId,
            FileUrl = submission?.File?.Url,
            Score = submission?.Score,
            Feedback = submission?.Feedback,
            SubmittedAt = submission?.CreatedAt,
            GradedAt = submission?.GradedAt
        };
    }

    public static StudentQuizDetailResponse ToQuizDetailResponse(Quiz quiz)
    {
        return new StudentQuizDetailResponse
        {
            Id = quiz.Id,
            Title = quiz.Title,
            CourseId = quiz.CourseId,
            CourseTitle = quiz.Course?.Title ?? string.Empty,
            Questions = quiz.QuizQuestions
                .OrderBy(q => q.Id)
                .Select(q => new StudentQuizQuestionResponse
                {
                    Id = q.Id,
                    Content = q.Content,
                    Answers = q.QuizAnswers
                        .OrderBy(a => a.Id)
                        .Select(a => new StudentQuizAnswerOptionResponse
                        {
                            Id = a.Id,
                            Content = a.Content
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    public static StudentQuizResultResponse ToQuizResultResponse(QuizAttempt attempt)
    {
        return new StudentQuizResultResponse
        {
            AttemptId = attempt.Id,
            QuizId = attempt.QuizId,
            QuizTitle = attempt.Quiz?.Title ?? string.Empty,
            TotalQuestions = attempt.TotalQuestions,
            CorrectAnswers = attempt.CorrectAnswers,
            Score = attempt.Score,
            SubmittedAt = attempt.SubmittedAt,
            Answers = attempt.Answers
                .OrderBy(a => a.QuestionId)
                .Select(a =>
                {
                    var correctAnswer = a.Question?.QuizAnswers.FirstOrDefault(answer => answer.IsCorrect);
                    return new StudentQuizResultAnswerResponse
                    {
                        QuestionId = a.QuestionId,
                        QuestionContent = a.Question?.Content ?? string.Empty,
                        SelectedAnswerId = a.AnswerId,
                        SelectedAnswerContent = a.Answer?.Content,
                        CorrectAnswerId = correctAnswer?.Id,
                        CorrectAnswerContent = correctAnswer?.Content,
                        IsCorrect = a.IsCorrect
                    };
                })
                .ToList()
        };
    }

    private static string GetSubmissionStatus(Submission? submission)
    {
        if (submission == null)
        {
            return "NotSubmitted";
        }

        return submission.Score.HasValue || submission.GradedAt.HasValue
            ? "Graded"
            : "Submitted";
    }
}
