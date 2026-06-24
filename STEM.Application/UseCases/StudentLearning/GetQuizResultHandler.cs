using STEM.Application.Dtos.StudentLearning;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.StudentLearning;

public class GetQuizResultHandler
{
    private readonly IQuizRepository _quizRepository;
    private readonly IQuizAttemptRepository _quizAttemptRepository;
    private readonly IUserRepository _userRepository;

    public GetQuizResultHandler(
        IQuizRepository quizRepository,
        IQuizAttemptRepository quizAttemptRepository,
        IUserRepository userRepository)
    {
        _quizRepository = quizRepository;
        _quizAttemptRepository = quizAttemptRepository;
        _userRepository = userRepository;
    }

    public async Task<StudentQuizResultResponse?> Handle(
        int quizId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        StudentLearningGuard.EnsureStudent(currentUser);

        var quiz = await _quizRepository.GetByIdWithDetailsAsync(quizId, cancellationToken);
        if (quiz == null)
        {
            throw new KeyNotFoundException("Quiz not found.");
        }

        if (!StudentLearningGuard.CanAccessQuiz(quiz, currentUserId))
        {
            throw new UnauthorizedAccessException("Student is not enrolled in this quiz course.");
        }

        var attempt = await _quizAttemptRepository.GetLatestByQuizAndStudentAsync(
            quizId,
            currentUserId,
            cancellationToken);

        return attempt == null ? null : StudentLearningMapper.ToQuizResultResponse(attempt);
    }
}
