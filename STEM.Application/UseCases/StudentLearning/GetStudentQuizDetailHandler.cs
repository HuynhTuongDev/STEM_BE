using STEM.Application.Dtos.StudentLearning;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.StudentLearning;

public class GetStudentQuizDetailHandler
{
    private readonly IQuizRepository _quizRepository;
    private readonly IUserRepository _userRepository;

    public GetStudentQuizDetailHandler(
        IQuizRepository quizRepository,
        IUserRepository userRepository)
    {
        _quizRepository = quizRepository;
        _userRepository = userRepository;
    }

    public async Task<StudentQuizDetailResponse> Handle(
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

        return StudentLearningMapper.ToQuizDetailResponse(quiz);
    }
}
