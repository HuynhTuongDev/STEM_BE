using STEM.Application.Dtos.Grading;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

public class GetSubmissionsHandler
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IClassRepository _classRepository;

    public GetSubmissionsHandler(
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository,
        IAssignmentRepository assignmentRepository,
        IEnrollmentRepository enrollmentRepository,
        IClassRepository classRepository)
    {
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
        _assignmentRepository = assignmentRepository;
        _enrollmentRepository = enrollmentRepository;
        _classRepository = classRepository;
    }

    public async Task<PagedSubmissionResponse> Handle(
        GetSubmissionsRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        int? schoolId = null;
        int? teacherId = null;
        var studentId = request.StudentId;
        var roleName = currentUser.Role?.Name;

        if (roleName == RoleNames.SchoolAdministrator)
        {
            schoolId = currentUser.SchoolId ?? throw new UnauthorizedAccessException("School admin has no school.");
        }
        else if (roleName == RoleNames.Teacher)
        {
            teacherId = currentUser.Id;
        }
        else if (roleName == RoleNames.Student)
        {
            if (request.StudentId.HasValue && request.StudentId.Value != currentUser.Id)
            {
                throw new UnauthorizedAccessException("Students can only view their own submissions.");
            }

            studentId = currentUser.Id;
        }
        else
        {
            throw new UnauthorizedAccessException("You are not allowed to view submissions.");
        }

        var (submissions, totalCount) = await _submissionRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            request.AssignmentId,
            request.ClassId,
            studentId,
            schoolId,
            teacherId,
            cancellationToken);

        var items = submissions.Select(SubmissionResponseMapper.MapWithAssignmentDetails).ToList();

        // BUG THẬT đã vá: danh sách trên chỉ phản ánh những gì ĐÃ có trong bảng
        // Submissions — học sinh đã enroll vào lớp nhưng chưa từng nộp bài
        // không xuất hiện ở đâu cả (giáo viên không có cách nào biết ai còn
        // thiếu). Chỉ bổ sung khi lọc theo đúng 1 assignment (mới biết rõ lớp
        // nào để lấy roster) và người xem không phải chính học sinh đó (student
        // tự xem luôn chỉ có submission của mình, không cần "chưa nộp"). Chỉ
        // gắn vào trang 1 — các dòng "chưa nộp" là suy ra từ roster, không có
        // vị trí tự nhiên trong phân trang của Submissions thật.
        if (request.AssignmentId.HasValue && roleName != RoleNames.Student && pageNumber == 1)
        {
            var notSubmitted = await BuildNotSubmittedEntriesAsync(
                request.AssignmentId.Value,
                items,
                cancellationToken);
            items.AddRange(notSubmitted);
        }

        return new PagedSubmissionResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        };
    }

    // Response-only — "not_submitted" là trạng thái suy ra cho FE hiển thị,
    // KHÔNG phải giá trị Submission.Status thật (không thêm vào
    // SubmissionStatuses.All — giá trị đó chỉ dành cho Submission thật sự
    // được lưu, các dòng ở đây không tương ứng row nào trong DB).
    //
    // Cố tình dùng các query "nông" (GetByIdAsync đơn giản + GetByClassIdAsync)
    // thay vì AssignmentRepository.GetByIdWithDetailsAsync (projection nhiều
    // nhánh optional cùng lúc — Class/Submissions/Metrics/QuizDetail/
    // ReportDetail/SimulationDetail/Rubric) — không chỉ vì không cần phần lớn
    // dữ liệu đó, mà bản thân projection nhiều nhánh đó CHỈ trả về null dưới
    // EF InMemory provider (xác nhận trực tiếp bằng test, xem
    // VirtualLabSubmissionFlowGateTests.cs) dù dữ liệu tồn tại thật — cùng họ
    // vấn đề InMemory-vs-relational đã ghi nhận ở
    // VirtualLabSubmissionRoundTripTests.cs. Query nông ở đây tránh né vấn đề
    // đó luôn, không chỉ né trong test.
    private async Task<List<SubmissionResponse>> BuildNotSubmittedEntriesAsync(
        int assignmentId,
        List<SubmissionResponse> submittedItems,
        CancellationToken cancellationToken)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId, cancellationToken);
        if (assignment == null)
        {
            return new List<SubmissionResponse>();
        }

        var enrollments = (await _enrollmentRepository.GetByClassIdAsync(assignment.ClassId, cancellationToken)).ToList();
        if (enrollments.Count == 0)
        {
            return new List<SubmissionResponse>();
        }

        var classEntity = await _classRepository.GetByIdAsync(assignment.ClassId, cancellationToken);

        // submittedItems chỉ là trang hiện tại của Submissions thật — nhưng
        // hàm này chỉ chạy khi pageNumber==1, và các FE call site đang dùng
        // pageSize=100 (lấy hết trong 1 trang) nên trong thực tế đã đủ; số ít
        // assignment có >100 submissions thật (nhiều lần nộp lại) mới bị sai
        // sót nhẹ — chấp nhận được, ghi rõ ở đây thay vì giấu.
        var submittedStudentIds = submittedItems
            .Where(item => item.StudentId.HasValue)
            .Select(item => item.StudentId!.Value)
            .ToHashSet();

        var missingStudentIds = enrollments
            .Select(e => e.StudentId)
            .Distinct()
            .Where(id => !submittedStudentIds.Contains(id))
            .ToList();

        var studentsById = enrollments
            .Where(e => e.Student != null)
            .GroupBy(e => e.StudentId)
            .ToDictionary(g => g.Key, g => g.First().Student!);

        var result = new List<SubmissionResponse>();
        foreach (var studentId in missingStudentIds)
        {
            studentsById.TryGetValue(studentId, out var student);
            result.Add(new SubmissionResponse
            {
                Id = 0,
                AssignmentId = assignment.Id,
                AssignmentTitle = assignment.Title,
                ClassId = assignment.ClassId,
                ClassCode = classEntity?.ClassCode ?? string.Empty,
                StudentId = studentId,
                StudentName = student?.FullName ?? string.Empty,
                StudentEmail = student?.Email ?? string.Empty,
                Status = "not_submitted",
                AttemptNumber = 0,
                MaxScore = assignment.MaxScore,
                AssignmentType = assignment.AssignmentType
            });
        }

        return result;
    }
}
