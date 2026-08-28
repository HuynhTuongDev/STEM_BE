using Microsoft.EntityFrameworkCore;
using STEM.Application.Dtos.Labs;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Users;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services;

public class LabSubmissionService : ILabSubmissionService
{
    // Marker trong Assignment.Description để tìm lại "Assignment ẩn" đã tạo
    // cho 1 cặp (Lab, Class) — không thêm cột DB nào, chỉ dùng field text có
    // sẵn làm khoá tra cứu xác định. Giáo viên không bao giờ thấy field này
    // (Assignment ẩn không xuất hiện trong /api/Assignments UI thường vì
    // Teacher chỉ thao tác qua Lab, không đi tìm Assignment).
    private const string HiddenMarkerPrefix = "[[hidden-lab-assignment:";
    private const string HiddenMarkerSuffix = "]]";

    private readonly StemDbContext _context;
    private readonly IVirtualLabRuntimeService _runtimeService;

    public LabSubmissionService(StemDbContext context, IVirtualLabRuntimeService runtimeService)
    {
        _context = context;
        _runtimeService = runtimeService;
    }

    public async Task<LabSubmissionListResponse> GetSubmissionsAsync(
        Guid labId,
        int classId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var lab = await _context.Labs
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == labId, cancellationToken)
            ?? throw new KeyNotFoundException("Lab not found.");

        var classEntity = await _context.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == classId, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found.");

        var currentUser = await _context.Users
            .AsNoTracking()
            .Include(item => item.Role)
            .FirstOrDefaultAsync(item => item.Id == currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user not found.");

        await EnsureCanManageAsync(currentUser, classEntity, labId, classId, cancellationToken);

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Where(item => item.ClassId == classId)
            .Include(item => item.Student)
            .ToListAsync(cancellationToken);

        var assignmentId = await FindAssignmentIdAsync(labId, classId, cancellationToken);

        var submissionsByStudent = new Dictionary<int, Submission>();
        if (assignmentId.HasValue)
        {
            var submissions = await _context.Submissions
                .AsNoTracking()
                .Where(item => item.AssignmentId == assignmentId.Value)
                .OrderByDescending(item => item.AttemptNumber)
                .ToListAsync(cancellationToken);

            // Giữ attempt MỚI NHẤT mỗi học sinh (OrderByDescending ở trên) —
            // roster chỉ cần trạng thái hiện tại, lịch sử attempt xem ở
            // Submission Detail.
            foreach (var submission in submissions)
            {
                if (submission.StudentId.HasValue && !submissionsByStudent.ContainsKey(submission.StudentId.Value))
                {
                    submissionsByStudent[submission.StudentId.Value] = submission;
                }
            }
        }

        var studentIds = enrollments.Select(item => item.StudentId).ToList();
        var progressByStudent = await _context.LabProgresses
            .AsNoTracking()
            .Where(item => item.LabId == labId && studentIds.Contains(item.StudentId))
            .ToDictionaryAsync(item => item.StudentId, cancellationToken);

        var rows = new List<LabSubmissionStudentRow>();
        var submittedCount = 0;

        foreach (var enrollment in enrollments)
        {
            submissionsByStudent.TryGetValue(enrollment.StudentId, out var submission);

            string status;
            if (submission != null)
            {
                status = submission.Status == SubmissionStatuses.Graded ? "graded" : "submitted";
                submittedCount++;
            }
            else
            {
                status = progressByStudent.ContainsKey(enrollment.StudentId) ? "in_progress" : "not_started";
            }

            rows.Add(new LabSubmissionStudentRow
            {
                StudentId = enrollment.StudentId,
                StudentName = enrollment.Student?.FullName ?? string.Empty,
                StudentEmail = enrollment.Student?.Email ?? string.Empty,
                Status = status,
                SubmissionId = submission?.Id,
                SubmittedAt = submission?.SubmittedAt,
                Score = submission?.Score ?? submission?.FinalScore,
                AttemptNumber = submission?.AttemptNumber,
            });
        }

        return new LabSubmissionListResponse
        {
            LabId = lab.Id.ToString("N"),
            LabTitle = lab.Title,
            ClassId = classEntity.Id,
            ClassCode = classEntity.ClassCode,
            TotalStudents = rows.Count,
            SubmittedCount = submittedCount,
            NotSubmittedCount = rows.Count - submittedCount,
            Students = rows,
        };
    }

    public async Task<VirtualLabSubmissionResponse> SubmitAsync(
        Guid labId,
        SubmitLabRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var lab = await _context.Labs
            .FirstOrDefaultAsync(item => item.Id == labId, cancellationToken)
            ?? throw new KeyNotFoundException("Lab not found.");

        // Học sinh phải thuộc 1 lớp mà Lab này được gán cho — tìm đúng lớp
        // đó để biết Assignment ẩn nào cần dùng (1 Lab có thể gán cho nhiều
        // lớp, mỗi lớp có Assignment ẩn RIÊNG — xem FindOrCreateAssignmentIdAsync).
        var classId = await _context.LabClassAssignments
            .AsNoTracking()
            .Where(item => item.LabId == labId)
            .Join(
                _context.Enrollments.AsNoTracking().Where(e => e.StudentId == currentUserId),
                lca => lca.ClassId,
                e => e.ClassId,
                (lca, e) => lca.ClassId)
            .FirstOrDefaultAsync(cancellationToken);

        if (classId == 0)
        {
            throw new UnauthorizedAccessException(
                "Bạn không thuộc lớp nào được gán phòng lab này — không thể nộp bài.");
        }

        var assignmentId = await FindOrCreateAssignmentIdAsync(lab, classId, cancellationToken);

        return await _runtimeService.SubmitVirtualLabAsync(
            new VirtualLabSubmissionRequest
            {
                AssignmentId = assignmentId,
                SessionId = request.SessionId,
                StudentId = currentUserId,
                DiagramJson = request.DiagramJson,
                SourceCode = request.SourceCode,
            },
            currentUserId,
            cancellationToken);
    }

    private async Task EnsureCanManageAsync(
        User currentUser,
        STEM.Core.Entities.Classes.Class classEntity,
        Guid labId,
        int classId,
        CancellationToken cancellationToken)
    {
        var roleName = currentUser.Role?.Name;

        var canManage = roleName switch
        {
            RoleNames.Teacher => classEntity.TeacherId == currentUser.Id,
            RoleNames.SchoolAdministrator => currentUser.SchoolId.HasValue && currentUser.SchoolId.Value == classEntity.SchoolId,
            _ => false,
        };

        if (!canManage)
        {
            throw new UnauthorizedAccessException("You are not allowed to view submissions for this class.");
        }

        var labAssignedToClass = await _context.LabClassAssignments
            .AsNoTracking()
            .AnyAsync(item => item.LabId == labId && item.ClassId == classId, cancellationToken);

        if (!labAssignedToClass)
        {
            throw new UnauthorizedAccessException("This lab is not assigned to this class.");
        }
    }

    private static string BuildMarker(Guid labId) => $"{HiddenMarkerPrefix}{labId:N}{HiddenMarkerSuffix}";

    private async Task<int?> FindAssignmentIdAsync(Guid labId, int classId, CancellationToken cancellationToken)
    {
        // Ưu tiên Lab.LinkedAssignmentId nếu giáo viên đã tự gán thủ công VÀ
        // đúng lớp đang xét (nhánh backward-compat — lab đã có Assignment
        // thật trước khi tính năng "ẩn" này tồn tại).
        var lab = await _context.Labs
            .AsNoTracking()
            .Where(item => item.Id == labId)
            .Select(item => new { item.LinkedAssignmentId })
            .FirstOrDefaultAsync(cancellationToken);

        if (lab?.LinkedAssignmentId.HasValue == true)
        {
            var linked = await _context.Assignments
                .AsNoTracking()
                .Where(item => item.Id == lab.LinkedAssignmentId.Value && item.ClassId == classId)
                .Select(item => (int?)item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (linked.HasValue)
            {
                return linked;
            }
        }

        var marker = BuildMarker(labId);
        return await _context.Assignments
            .AsNoTracking()
            .Where(item => item.ClassId == classId && item.Description == marker)
            .Select(item => (int?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<int> FindOrCreateAssignmentIdAsync(
        STEM.Core.Entities.Simulations.Lab lab,
        int classId,
        CancellationToken cancellationToken)
    {
        var existing = await FindAssignmentIdAsync(lab.Id, classId, cancellationToken);
        if (existing.HasValue)
        {
            return existing.Value;
        }

        // Chưa có Assignment nào cho cặp (Lab, Class) này — tự tạo 1 cái ẩn.
        // Race an toàn: unique constraint không tồn tại ở đây, nhưng 2 học
        // sinh CÙNG lớp submit lần đầu gần như đồng thời là biên hiếm; nếu
        // xảy ra sẽ tạo 2 Assignment trùng — chấp nhận được cho v1, không
        // thêm unique index (schema change) chỉ để chặn 1 race hiếm.
        var hiddenAssignment = new Assignment
        {
            ClassId = classId,
            Title = lab.Title,
            Description = BuildMarker(lab.Id),
            AssignmentType = STEM.Core.Entities.Projects.AssignmentTypes.PracticalSimulation,
            MaxScore = 100,
            AllowResubmit = true,
            Status = STEM.Core.Entities.Projects.AssignmentStatuses.Published,
            CreatedById = lab.CreatedById,
        };

        _context.Assignments.Add(hiddenAssignment);
        await _context.SaveChangesAsync(cancellationToken);

        // Nếu Lab chưa từng gán Assignment nào, lưu luôn làm mặc định — lần
        // sau FindAssignmentIdAsync có thể tìm nhanh qua LinkedAssignmentId
        // trước khi phải quét theo marker (chỉ optimize, không đổi hành vi).
        if (!lab.LinkedAssignmentId.HasValue)
        {
            lab.LinkedAssignmentId = hiddenAssignment.Id;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return hiddenAssignment.Id;
    }
}
