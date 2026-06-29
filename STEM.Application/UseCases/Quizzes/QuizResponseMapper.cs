using STEM.Application.Dtos.Quizzes;
using STEM.Core.Entities.Quizzes;

namespace STEM.Application.UseCases.Quizzes;

internal static class QuizResponseMapper
{
    public static QuizResponse Map(Quiz quiz, bool includeQuestions)
    {
        var course = quiz.Course;

        return new QuizResponse
        {
            Id = quiz.Id,
            CourseId = quiz.CourseId,
            CourseTitle = course?.Title ?? string.Empty,
            TeacherId = course?.TeacherId ?? 0,
            TeacherName = course?.Teacher?.FullName ?? string.Empty,
            SchoolId = course?.SchoolId,
            SchoolName = course?.School?.Name,
            Title = quiz.Title,
            QuestionsCount = quiz.QuizQuestions.Count,
            Questions = includeQuestions
                ? quiz.QuizQuestions
                    .OrderBy(question => question.Id)
                    .Select(MapQuestion)
                    .ToList()
                : Array.Empty<QuizQuestionResponse>(),
            CreatedAt = quiz.CreatedAt,
            UpdatedAt = quiz.UpdatedAt
        };
    }

    private static QuizQuestionResponse MapQuestion(QuizQuestion question)
    {
        return new QuizQuestionResponse
        {
            Id = question.Id,
            Content = question.Content,
            Answers = question.QuizAnswers
                .OrderBy(answer => answer.Id)
                .Select(answer => new QuizAnswerResponse
                {
                    Id = answer.Id,
                    Content = answer.Content,
                    IsCorrect = answer.IsCorrect
                })
                .ToList()
        };
    }
}
