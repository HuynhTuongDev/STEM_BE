using STEM.Core.Repository;

namespace STEM.Application.UseCases.Quizzes;

public class DeleteQuizHandler
{
    private readonly IQuizRepository _quizRepository;
    private readonly IUserRepository _userRepository;

    public DeleteQuizHandler(
        IQuizRepository quizRepository,
        IUserRepository userRepository)
    {
        _quizRepository = quizRepository;
        _userRepository = userRepository;
    }

    public async Task Handle(
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

        if (quiz.Course == null || !QuizAuthorization.CanManageCourse(currentUser, quiz.Course))
        {
            throw new UnauthorizedAccessException("You are not allowed to delete this quiz.");
        }

        _quizRepository.Delete(quiz);
        await _quizRepository.SaveChangesAsync(cancellationToken);
    }
}
