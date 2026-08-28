using System.Text.Json;
using STEM.Application.Dtos.Assignments;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Assessments;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Assignments;

public class CreateAssignmentHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly INotificationService _notificationService;

    public CreateAssignmentHandler(
        IAssignmentRepository assignmentRepository,
        IClassRepository classRepository,
        IUserRepository userRepository,
        IEnrollmentRepository enrollmentRepository,
        INotificationService notificationService)
    {
        _assignmentRepository = assignmentRepository;
        _classRepository = classRepository;
        _userRepository = userRepository;
        _enrollmentRepository = enrollmentRepository;
        _notificationService = notificationService;
    }

    public async Task<AssignmentResponse> Handle(
        CreateAssignmentRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        AssignmentRequestMapper.ValidateBase(
            request.ClassId,
            request.Title,
            request.AssignmentType,
            request.Status,
            request.MaxScore);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var classEntity = await _classRepository.GetByIdSummaryAsync(request.ClassId, cancellationToken);
        if (classEntity == null)
        {
            throw new KeyNotFoundException("Class not found.");
        }

        if (!AssignmentAuthorization.CanManageClass(currentUser, classEntity))
        {
            throw new UnauthorizedAccessException("You are not allowed to create assignments for this class.");
        }

        var now = DateTime.UtcNow;
        var assignment = new Assignment();
        AssignmentRequestMapper.ApplyBase(assignment, request, currentUser.Id, now);
        AssignmentRequestMapper.ApplyDetails(assignment, request, now);

        // Create Rubric if rubric criteria are provided
        Rubric? rubric = null;
        if (request.RubricCriteria != null && request.RubricCriteria.Count > 0)
        {
            rubric = new Rubric
            {
                Criteria = JsonSerializer.Serialize(request.RubricCriteria),
                MaxScore = request.RubricCriteria.Sum(c => c.MaxPoints),
                CreatedAt = now,
                UpdatedAt = now
            };
            assignment.Rubric = rubric;
        }

        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        // N-17: Notify students about new assignment
        if (request.Status == "published")
        {
            var enrollments = await _enrollmentRepository.GetByClassIdAsync(request.ClassId, cancellationToken);
            var studentIds = enrollments.Select(e => e.StudentId).ToList();

            var assignmentTypeText = assignment.AssignmentType switch
            {
                "Quiz" => "bài Quiz",
                "Report" => "bài báo cáo",
                "PracticalSimulation" => "bài lab thực hành",
                _ => "bài tập"
            };

            var dueDateText = assignment.DueDate.HasValue
                ? $". Hạn nộp: {assignment.DueDate.Value:dd/MM/yyyy HH:mm}"
                : "";

            var title = $"Bài tập mới: {assignment.Title}";
            var content = $"Giáo viên đã giao cho bạn {assignmentTypeText} \"{assignment.Title}\"{dueDateText}.";

            await _notificationService.SendToManyAsync(studentIds, title, content, NotificationType.AssignmentAssigned, cancellationToken);
        }

        return AssignmentResponseMapper.Map(assignment, classEntity);
    }
}
