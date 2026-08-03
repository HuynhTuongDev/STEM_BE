using STEM.Application.Dtos.Quizzes;
using STEM.Core.Entities.Quizzes;

namespace STEM.Application.UseCases.Quizzes;

internal static class QuizResponseMapper
{
    public static QuizResponse Map(Quiz quiz, bool includeQuestions)
    {
        var classEntity = quiz.Class;

        return new QuizResponse
        {
            Id = quiz.Id,
            ClassId = quiz.ClassId,
            ClassCode = classEntity?.ClassCode ?? string.Empty,
            CourseId = classEntity?.CourseId ?? 0,
            CourseTitle = classEntity?.Course?.Title ?? string.Empty,
            TeacherId = classEntity?.TeacherId ?? 0,
            TeacherName = classEntity?.Teacher?.FullName ?? string.Empty,
            SchoolId = classEntity?.SchoolId ?? 0,
            SchoolName = classEntity?.School?.Name ?? string.Empty,
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
