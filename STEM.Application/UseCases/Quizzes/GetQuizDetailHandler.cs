using STEM.Application.Dtos.Quizzes;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Quizzes;

public class GetQuizDetailHandler
{
    private readonly IQuizRepository _quizRepository;
    private readonly IUserRepository _userRepository;

    public GetQuizDetailHandler(
        IQuizRepository quizRepository,
        IUserRepository userRepository)
    {
        _quizRepository = quizRepository;
        _userRepository = userRepository;
    }

    public async Task<QuizResponse> Handle(
        int quizId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var quiz = await _quizRepository.GetByIdWithDetailsAsync(quizId, cancellationToken);
        if (quiz == null)
        {
            throw new KeyNotFoundException("Quiz not found.");
        }

        if (!QuizAuthorization.CanViewQuiz(currentUser, quiz))
        {
            throw new UnauthorizedAccessException("You are not allowed to view this quiz.");
        }

        return QuizResponseMapper.Map(quiz, includeQuestions: true);
    }
}
