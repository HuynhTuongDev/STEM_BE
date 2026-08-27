using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using STEM.Application.Dtos.Labs;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Users;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services;

public class LabService : ILabService
{
    private const string PrivateOrMissingProjectMessage =
        "Project không tồn tại hoặc đang ở chế độ riêng tư, vui lòng public/unlisted project trước khi dán link.";

    private static readonly Regex ProjectIdRegex = new("^[0-9]+$", RegexOptions.Compiled);
    private static readonly Regex ProjectUrlRegex = new(
        @"(?:^|/)projects/(?<id>[0-9]+)(?:[/?#]|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly StemDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<LabService> _logger;
    private readonly IPrecompileTriggerService _precompileTrigger;

    public LabService(
        StemDbContext context,
        HttpClient httpClient,
        ILogger<LabService> logger,
        IPrecompileTriggerService precompileTrigger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
        _precompileTrigger = precompileTrigger;
    }

    public async Task<PagedLabResponse> GetLabsAsync(
        GetLabsRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var query = BuildLabQuery(asNoTracking: true);
        query = ApplyViewScope(query, currentUser);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(lab =>
                lab.Title.ToLower().Contains(term) ||
                lab.Description.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim().ToLower();
            query = query.Where(lab => lab.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim().ToLower();
            query = query.Where(lab => lab.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.SimulationMode))
        {
            var simulationMode = request.SimulationMode.Trim().ToLower();
            query = query.Where(lab => lab.SimulationMode == simulationMode);
        }

        if (request.ClassId.HasValue)
        {
            query = query.Where(lab =>
                lab.ClassAssignments.Any(assignment => assignment.ClassId == request.ClassId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var labs = await query
            .OrderByDescending(lab => lab.UpdatedAt)
            .ThenBy(lab => lab.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedLabResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = labs.Select(MapLab).ToList()
        };
    }

    public async Task<LabResponse> GetLabAsync(
        Guid id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        var lab = await BuildLabQuery(asNoTracking: true)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (lab == null)
        {
            throw new KeyNotFoundException("Lab not found.");
        }

        if (!CanViewLab(currentUser, lab))
        {
            throw new UnauthorizedAccessException("You are not allowed to view this lab.");
        }

        return MapLab(lab);
    }

    public async Task<LabResponse> CreateLabAsync(
        CreateLabRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        EnsureCanManageLabs(currentUser);

        var payload = PreparePayload(request);
        var now = DateTime.UtcNow;

        await EnsureCanUseClassesAsync(currentUser, payload.ClassIds, payload.Status, cancellationToken);
        await EnsureCanLinkAssignmentAsync(currentUser, payload.LinkedAssignmentId, payload.ClassIds, cancellationToken);
        await EnsureAllowedComponentsSupportedAsync(payload.AllowedComponentTypes, cancellationToken);

        var lab = new Lab
        {
            Id = Guid.NewGuid(),
            Title = payload.Title,
            Description = payload.Description,
            Category = payload.Category,
            ThumbnailUrl = payload.ThumbnailUrl,
            SimulationMode = payload.SimulationMode,
            BoardType = payload.BoardType,
            StarterCode = payload.StarterCode,
            CircuitConfigJson = payload.CircuitConfigJson,
            AllowedComponentTypesJson = payload.AllowedComponentTypesJson,
            WokwiProjectId = payload.WokwiProjectId,
            WokwiProjectUrl = payload.WokwiProjectUrl,
            Status = payload.Status,
            LinkedAssignmentId = payload.LinkedAssignmentId,
            CreatedById = currentUser.Id,
            CreatedAt = now,
            UpdatedAt = now,
            ClassAssignments = payload.ClassIds
                .Select(classId => new LabClassAssignment
                {
                    Id = Guid.NewGuid(),
                    ClassId = classId,
                    ScheduleId = payload.ScheduleId,
                    CreatedAt = now
                })
                .ToList()
        };

        _context.Labs.Add(lab);
        await _context.SaveChangesAsync(cancellationToken);

        TriggerStarterCodePrecompile(lab.Id, lab.BoardType, lab.StarterCode);

        var savedLab = await BuildLabQuery(asNoTracking: true)
            .FirstAsync(item => item.Id == lab.Id, cancellationToken);
        return MapLab(savedLab);
    }

    public async Task<LabResponse> UpdateLabAsync(
        Guid id,
        UpdateLabRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        EnsureCanManageLabs(currentUser);

        // Retry logic for optimistic concurrency
        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await UpdateLabInternalAsync(id, request, currentUser, cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict on lab {LabId}, attempt {Attempt}", id, attempt + 1);
                if (attempt == maxRetries - 1)
                {
                    throw new InvalidOperationException(
                        "Lab was modified by another user. Please refresh and try again.", ex);
                }
                // Detach all entries to clear EF tracking cache
                _context.ChangeTracker.Clear();
                await Task.Delay(100 * (attempt + 1), cancellationToken);
            }
        }
        throw new InvalidOperationException("Failed to update lab after retries.");
    }

    private async Task<LabResponse> UpdateLabInternalAsync(
        Guid id,
        UpdateLabRequest request,
        User currentUser,
        CancellationToken cancellationToken)
    {
        var lab = await BuildLabQuery(asNoTracking: false)
            .Include(l => l.ClassAssignments)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (lab == null)
        {
            throw new KeyNotFoundException("Lab not found.");
        }

        if (!CanManageLab(currentUser, lab))
        {
            throw new UnauthorizedAccessException("You are not allowed to update this lab.");
        }

        var payload = PreparePayload(request);
        await EnsureCanUseClassesAsync(currentUser, payload.ClassIds, payload.Status, cancellationToken);
        await EnsureCanLinkAssignmentAsync(currentUser, payload.LinkedAssignmentId, payload.ClassIds, cancellationToken);
        await EnsureAllowedComponentsSupportedAsync(payload.AllowedComponentTypes, cancellationToken);

        lab.Title = payload.Title;
        lab.Description = payload.Description;
        lab.Category = payload.Category;
        lab.ThumbnailUrl = payload.ThumbnailUrl;
        lab.SimulationMode = payload.SimulationMode;
        lab.BoardType = payload.BoardType;
        lab.StarterCode = payload.StarterCode;
        lab.CircuitConfigJson = payload.CircuitConfigJson;
        lab.AllowedComponentTypesJson = payload.AllowedComponentTypesJson;
        lab.WokwiProjectId = payload.WokwiProjectId;
        lab.WokwiProjectUrl = payload.WokwiProjectUrl;
        lab.Status = payload.Status;
        lab.LinkedAssignmentId = payload.LinkedAssignmentId;
        lab.UpdatedAt = DateTime.UtcNow;

        SyncClassAssignments(lab, payload.ClassIds, payload.ScheduleId);
        await _context.SaveChangesAsync(cancellationToken);

        TriggerStarterCodePrecompile(lab.Id, lab.BoardType, lab.StarterCode);

        var savedLab = await BuildLabQuery(asNoTracking: true)
            .FirstAsync(item => item.Id == id, cancellationToken);
        return MapLab(savedLab);
    }

    // Precompile firmware cho starterCode NGAY khi giáo viên lưu bài — để lần
    // Run đầu tiên của BẤT KỲ học sinh nào (chưa sửa gì code) rơi đúng vào cache
    // hit của IFirmwareCacheService (khoá theo nội dung, không theo project) và
    // chạy nhanh như Wokwi, không phải chờ compile ~40-90s. Chỉ ESP32 (QEMU
    // runner không hỗ trợ AVR/Arduino Uno — xem QemuEsp32Runner.IsAvrBoard).
    // Logic chạy nền thật (scope riêng, không throw, tự check cache trước khi
    // compile) nằm ở IPrecompileTriggerService — dùng chung với endpoint
    // precompile lúc học sinh đang gõ code (VirtualLabProjectController).
    private void TriggerStarterCodePrecompile(Guid labId, string boardType, string? starterCode)
    {
        if (string.IsNullOrWhiteSpace(starterCode) ||
            !string.Equals(boardType, LabBoardTypes.Esp32DevkitV1, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var board = SimulationCompileService.NormalizeBoard(boardType);
        _precompileTrigger.TriggerBackgroundCompile(starterCode, board, "arduino", buildCacheScopeId: labId);
    }

    public async Task DeleteLabAsync(
        Guid id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        EnsureCanManageLabs(currentUser);

        var lab = await BuildLabQuery(asNoTracking: false)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (lab == null)
        {
            throw new KeyNotFoundException("Lab not found.");
        }

        if (!CanManageLab(currentUser, lab))
        {
            throw new UnauthorizedAccessException("You are not allowed to delete this lab.");
        }

        _context.Labs.Remove(lab);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ValidateWokwiProjectResponse> ValidateWokwiProjectAsync(
        ValidateWokwiProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var (projectId, projectUrl) = NormalizeWokwiProject(request.WokwiProjectId, request.WokwiProjectUrl);
        return await ProbeWokwiProjectAsync(projectId, projectUrl, cancellationToken);
    }

    public async Task<ValidateWokwiProjectResponse> ValidateExistingWokwiProjectAsync(
        Guid id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        var lab = await BuildLabQuery(asNoTracking: true)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (lab == null)
        {
            throw new KeyNotFoundException("Lab not found.");
        }

        if (!CanManageLab(currentUser, lab))
        {
            throw new UnauthorizedAccessException("You are not allowed to validate this lab.");
        }

        var (projectId, projectUrl) = NormalizeWokwiProject(lab.WokwiProjectId, lab.WokwiProjectUrl);
        return await ProbeWokwiProjectAsync(projectId, projectUrl, cancellationToken);
    }

    public async Task<LabProgressResponse> StartProgressAsync(
        Guid id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        EnsureStudent(currentUser);

        var lab = await BuildLabQuery(asNoTracking: true)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (lab == null)
        {
            throw new KeyNotFoundException("Lab not found.");
        }

        if (!CanViewLab(currentUser, lab))
        {
            throw new UnauthorizedAccessException("You are not allowed to start this lab.");
        }

        var now = DateTime.UtcNow;
        var progress = await _context.LabProgresses
            .FirstOrDefaultAsync(item => item.LabId == id && item.StudentId == currentUser.Id, cancellationToken);

        if (progress == null)
        {
            progress = new LabProgress
            {
                Id = Guid.NewGuid(),
                LabId = id,
                StudentId = currentUser.Id,
                StartedAt = now,
                LastOpenedAt = now,
                OpenCount = 1
            };
            _context.LabProgresses.Add(progress);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return MapProgress(progress);
            }
            catch (DbUpdateException ex) when (IsDuplicateKey(ex))
            {
                // Race: a concurrent progress/start call for the same (LabId, StudentId)
                // (e.g. React StrictMode's double-invoked effect) already inserted the
                // row first — the unique index on (LabId, StudentId) rejects this insert.
                // Fall back to updating the row the other request just created instead
                // of surfacing the raw 500.
                _context.Entry(progress).State = EntityState.Detached;
                progress = await _context.LabProgresses
                    .FirstOrDefaultAsync(item => item.LabId == id && item.StudentId == currentUser.Id, cancellationToken)
                    ?? throw new InvalidOperationException(
                        "LabProgress not found after a duplicate-key conflict on create.");
                progress.LastOpenedAt = now;
                progress.OpenCount += 1;
            }
        }
        else
        {
            progress.LastOpenedAt = now;
            progress.OpenCount += 1;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapProgress(progress);
    }

    private static bool IsDuplicateKey(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: "23505" };

    public async Task<LabProgressResponse> CompleteProgressAsync(
        Guid id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        EnsureStudent(currentUser);

        var lab = await BuildLabQuery(asNoTracking: true)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (lab == null)
        {
            throw new KeyNotFoundException("Lab not found.");
        }

        if (!CanViewLab(currentUser, lab))
        {
            throw new UnauthorizedAccessException("You are not allowed to complete this lab.");
        }

        if (lab.LinkedAssignmentId.HasValue)
        {
            throw new InvalidOperationException("This lab uses a linked assignment for completion.");
        }

        var now = DateTime.UtcNow;
        var progress = await _context.LabProgresses
            .FirstOrDefaultAsync(item => item.LabId == id && item.StudentId == currentUser.Id, cancellationToken);

        if (progress == null)
        {
            progress = new LabProgress
            {
                Id = Guid.NewGuid(),
                LabId = id,
                StudentId = currentUser.Id,
                StartedAt = now,
                LastOpenedAt = now,
                OpenCount = 1
            };
            _context.LabProgresses.Add(progress);
        }

        progress.LastOpenedAt = now;
        if (!progress.CompletedAt.HasValue)
        {
            progress.CompletedAt = now;
            progress.DurationSeconds = Math.Max(0, (int)(now - progress.StartedAt).TotalSeconds);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapProgress(progress);
    }

    public async Task<IReadOnlyCollection<LabProgressResponse>> GetMyProgressAsync(
        int? classId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        EnsureStudent(currentUser);

        var query = _context.LabProgresses
            .AsNoTracking()
            .Where(item => item.StudentId == currentUser.Id);

        if (classId.HasValue)
        {
            query = query.Where(item =>
                item.Lab!.ClassAssignments.Any(assignment => assignment.ClassId == classId.Value));
        }

        var progresses = await query.ToListAsync(cancellationToken);
        return progresses.Select(MapProgress).ToList();
    }

    public async Task<LabStatsResponse> GetStatsAsync(
        Guid id,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(currentUserId, cancellationToken);
        var lab = await BuildLabQuery(asNoTracking: true)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (lab == null)
        {
            throw new KeyNotFoundException("Lab not found.");
        }

        if (!CanManageLab(currentUser, lab))
        {
            throw new UnauthorizedAccessException("You are not allowed to view lab stats.");
        }

        return await BuildStatsAsync(lab, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ComponentGlueRegistryResponse>> GetComponentGlueRegistryAsync(
        bool supportedOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ComponentGlueRegistry
            .AsNoTracking()
            .AsQueryable();

        if (supportedOnly)
        {
            query = query.Where(component => component.Supported);
        }

        var components = await query
            .OrderBy(component => component.ComponentType)
            .ToListAsync(cancellationToken);

        return components
            .Select(component => new ComponentGlueRegistryResponse
            {
                ComponentType = component.ComponentType,
                Label = component.Label,
                Supported = component.Supported,
                PinRequirements = ParseJson(component.PinRequirementsJson, "{}"),
                UpdatedAt = component.UpdatedAt
            })
            .ToList();
    }

    private IQueryable<Lab> BuildLabQuery(bool asNoTracking)
    {
        var query = _context.Labs
            .Include(lab => lab.CreatedBy)
            .Include(lab => lab.LinkedAssignment)
            .Include(lab => lab.ClassAssignments)
                .ThenInclude(assignment => assignment.Class)
                    .ThenInclude(classEntity => classEntity!.Course)
            .Include(lab => lab.ClassAssignments)
                .ThenInclude(assignment => assignment.Class)
                    .ThenInclude(classEntity => classEntity!.Enrollments)
            .Include(lab => lab.Progresses)
            .AsQueryable();

        return asNoTracking ? query.AsNoTracking() : query;
    }

    private IQueryable<Lab> ApplyViewScope(IQueryable<Lab> query, User user)
    {
        var roleName = user.Role?.Name;

        if (roleName == RoleNames.SchoolAdministrator)
        {
            var schoolId = user.SchoolId ?? throw new UnauthorizedAccessException("School admin has no school.");
            return query.Where(lab =>
                lab.CreatedBy != null && lab.CreatedBy.SchoolId == schoolId ||
                lab.ClassAssignments.Any(assignment =>
                    assignment.Class != null && assignment.Class.SchoolId == schoolId));
        }

        if (roleName == RoleNames.Teacher)
        {
            return query.Where(lab =>
                lab.CreatedById == user.Id ||
                lab.ClassAssignments.Any(assignment =>
                    assignment.Class != null && assignment.Class.TeacherId == user.Id));
        }

        if (roleName == RoleNames.Student)
        {
            return query.Where(lab =>
                lab.Status == LabStatuses.Published &&
                lab.ClassAssignments.Any(assignment =>
                    assignment.Class != null &&
                    assignment.Class.Enrollments.Any(enrollment => enrollment.StudentId == user.Id)));
        }

        throw new UnauthorizedAccessException("You are not allowed to view labs.");
    }

    private async Task<User> GetCurrentUserAsync(int currentUserId, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(item => item.Role)
            .FirstOrDefaultAsync(item => item.Id == currentUserId, cancellationToken);

        return user ?? throw new UnauthorizedAccessException("Current user not found.");
    }

    private static void EnsureCanManageLabs(User user)
    {
        if (user.Role?.Name is not (RoleNames.SchoolAdministrator or RoleNames.Teacher))
        {
            throw new UnauthorizedAccessException("You are not allowed to manage labs.");
        }
    }

    private static void EnsureStudent(User user)
    {
        if (user.Role?.Name != RoleNames.Student)
        {
            throw new UnauthorizedAccessException("Only students can update lab progress.");
        }
    }

    private static bool CanViewLab(User user, Lab lab)
    {
        if (CanManageLab(user, lab))
        {
            return true;
        }

        return user.Role?.Name == RoleNames.Student &&
            lab.Status == LabStatuses.Published &&
            lab.ClassAssignments.Any(assignment =>
                assignment.Class?.Enrollments.Any(enrollment => enrollment.StudentId == user.Id) == true);
    }

    private static bool CanManageLab(User user, Lab lab)
    {
        var roleName = user.Role?.Name;

        if (roleName == RoleNames.SchoolAdministrator)
        {
            return user.SchoolId.HasValue &&
                (lab.CreatedBy?.SchoolId == user.SchoolId!.Value ||
                 lab.ClassAssignments.Any(assignment => assignment.Class?.SchoolId == user.SchoolId!.Value));
        }

        if (roleName == RoleNames.Teacher)
        {
            return lab.CreatedById == user.Id ||
                lab.ClassAssignments.Any(assignment => assignment.Class?.TeacherId == user.Id);
        }

        return false;
    }

    private static bool CanManageClass(User user, Class classEntity)
    {
        var roleName = user.Role?.Name;

        if (roleName == RoleNames.SchoolAdministrator)
        {
            return user.SchoolId.HasValue && classEntity.SchoolId == user.SchoolId!.Value;
        }

        if (roleName == RoleNames.Teacher)
        {
            return classEntity.TeacherId == user.Id;
        }

        return false;
    }

    private async Task EnsureCanUseClassesAsync(
        User currentUser,
        IReadOnlyCollection<int> classIds,
        string status,
        CancellationToken cancellationToken)
    {
        if (status == LabStatuses.Published && classIds.Count == 0)
        {
            throw new ArgumentException("ClassIds is required when publishing a lab.");
        }

        if (classIds.Count == 0)
        {
            return;
        }

        var classes = await _context.Classes
            .Where(classEntity => classIds.Contains(classEntity.Id))
            .ToListAsync(cancellationToken);

        if (classes.Count != classIds.Count)
        {
            throw new KeyNotFoundException("One or more classes were not found.");
        }

        if (classes.Any(classEntity => !CanManageClass(currentUser, classEntity)))
        {
            throw new UnauthorizedAccessException("You are not allowed to assign one or more classes.");
        }
    }

    private async Task EnsureCanLinkAssignmentAsync(
        User currentUser,
        int? linkedAssignmentId,
        IReadOnlyCollection<int> classIds,
        CancellationToken cancellationToken)
    {
        if (!linkedAssignmentId.HasValue)
        {
            return;
        }

        var assignment = await _context.Assignments
            .Include(item => item.Class)
            .FirstOrDefaultAsync(item => item.Id == linkedAssignmentId.Value, cancellationToken);

        if (assignment == null)
        {
            throw new KeyNotFoundException("Linked assignment not found.");
        }

        if (assignment.Class == null || !CanManageClass(currentUser, assignment.Class))
        {
            throw new UnauthorizedAccessException("You are not allowed to link this assignment.");
        }

        if (classIds.Count > 0 && !classIds.Contains(assignment.ClassId))
        {
            throw new ArgumentException("Linked assignment must belong to one of the assigned classes.");
        }

        if (assignment.AssignmentType != AssignmentTypes.Quiz &&
            assignment.AssignmentType != AssignmentTypes.TextReport &&
            assignment.AssignmentType != AssignmentTypes.PracticalSimulation)
        {
            throw new ArgumentException("Linked assignment must be a quiz, text report, or practical simulation assignment.");
        }
    }

    private async Task EnsureAllowedComponentsSupportedAsync(
        IReadOnlyCollection<string> componentTypes,
        CancellationToken cancellationToken)
    {
        if (componentTypes.Count == 0)
        {
            return;
        }

        var supportedTypes = await _context.ComponentGlueRegistry
            .AsNoTracking()
            .Where(component => component.Supported && componentTypes.Contains(component.ComponentType))
            .Select(component => component.ComponentType)
            .ToListAsync(cancellationToken);

        var missingTypes = componentTypes
            .Except(supportedTypes, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missingTypes.Count > 0)
        {
            throw new ArgumentException($"Unsupported component types: {string.Join(", ", missingTypes)}.");
        }
    }

    private static LabPayload PreparePayload(CreateLabRequest request)
    {
        return PreparePayload(
            request.Title,
            request.Description,
            request.Category,
            request.ThumbnailUrl,
            request.SimulationMode,
            request.BoardType,
            request.StarterCode,
            request.CircuitConfig,
            request.AllowedComponentTypes,
            request.WokwiProjectId,
            request.WokwiProjectUrl,
            request.ClassIds,
            request.Status,
            request.LinkedAssignmentId,
            request.ScheduleId);
    }

    private static LabPayload PreparePayload(UpdateLabRequest request)
    {
        return PreparePayload(
            request.Title,
            request.Description,
            request.Category,
            request.ThumbnailUrl,
            request.SimulationMode,
            request.BoardType,
            request.StarterCode,
            request.CircuitConfig,
            request.AllowedComponentTypes,
            request.WokwiProjectId,
            request.WokwiProjectUrl,
            request.ClassIds,
            request.Status,
            request.LinkedAssignmentId,
            request.ScheduleId);
    }

    private static LabPayload PreparePayload(
        string title,
        string description,
        string category,
        string thumbnailUrl,
        string simulationMode,
        string boardType,
        string? starterCode,
        JsonElement circuitConfig,
        IReadOnlyCollection<string> allowedComponentTypes,
        string? wokwiProjectId,
        string? wokwiProjectUrl,
        IReadOnlyCollection<int> classIds,
        string status,
        int? linkedAssignmentId,
        int? scheduleId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.");
        }

        var normalizedCategory = category.Trim().ToLower();
        if (!LabCategories.All.Contains(normalizedCategory))
        {
            throw new ArgumentException("Category must be physics, chemistry, biology, or robotics.");
        }

        var normalizedStatus = string.IsNullOrWhiteSpace(status)
            ? LabStatuses.Draft
            : status.Trim().ToLower();
        if (!LabStatuses.All.Contains(normalizedStatus))
        {
            throw new ArgumentException("Status must be draft or published.");
        }

        var normalizedSimulationMode = string.IsNullOrWhiteSpace(simulationMode)
            ? LabSimulationModes.WokwiIframe
            : simulationMode.Trim().ToLower();
        if (!LabSimulationModes.All.Contains(normalizedSimulationMode))
        {
            throw new ArgumentException("SimulationMode must be wokwi_iframe or custom_sandbox.");
        }

        var normalizedBoardType = string.IsNullOrWhiteSpace(boardType)
            ? LabBoardTypes.ArduinoUno
            : boardType.Trim().ToLower();
        if (!LabBoardTypes.All.Contains(normalizedBoardType))
        {
            throw new ArgumentException($"BoardType must be {string.Join(" or ", LabBoardTypes.All)}.");
        }

        var circuitConfigJson = circuitConfig.ValueKind == JsonValueKind.Undefined ||
            circuitConfig.ValueKind == JsonValueKind.Null
                ? "{}"
                : circuitConfig.GetRawText();

        ValidateCircuitConfig(circuitConfigJson);

        var normalizedAllowedComponents = (allowedComponentTypes ?? Array.Empty<string>())
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Select(type => type.Trim().ToLower())
            .Distinct()
            .ToList();
        var allowedComponentTypesJson = JsonSerializer.Serialize(normalizedAllowedComponents);

        string? projectId = null;
        string? projectUrl = null;
        if (normalizedSimulationMode == LabSimulationModes.WokwiIframe)
        {
            var normalizedProject = NormalizeWokwiProject(wokwiProjectId, wokwiProjectUrl);
            projectId = normalizedProject.ProjectId;
            projectUrl = normalizedProject.ProjectUrl;
        }

        var normalizedClassIds = (classIds ?? Array.Empty<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        return new LabPayload(
            title.Trim(),
            description?.Trim() ?? string.Empty,
            normalizedCategory,
            thumbnailUrl?.Trim() ?? string.Empty,
            normalizedSimulationMode,
            normalizedBoardType,
            string.IsNullOrWhiteSpace(starterCode) ? null : starterCode,
            circuitConfigJson,
            allowedComponentTypesJson,
            normalizedAllowedComponents,
            projectId,
            projectUrl,
            normalizedClassIds,
            normalizedStatus,
            linkedAssignmentId,
            scheduleId);
    }

    private static (string ProjectId, string ProjectUrl) NormalizeWokwiProject(
        string? wokwiProjectId,
        string? wokwiProjectUrl)
    {
        var projectId = wokwiProjectId?.Trim();
        var projectUrl = wokwiProjectUrl?.Trim();

        if (string.IsNullOrWhiteSpace(projectId) && !string.IsNullOrWhiteSpace(projectUrl))
        {
            projectId = ExtractProjectIdFromUrl(projectUrl);
        }

        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new ArgumentException("WokwiProjectId or WokwiProjectUrl is required.");
        }

        if (!ProjectIdRegex.IsMatch(projectId))
        {
            throw new ArgumentException("WokwiProjectId is invalid.");
        }

        return (projectId, $"https://wokwi.com/projects/{projectId}");
    }

    private static void ValidateCircuitConfig(string circuitConfigJson)
    {
        try
        {
            using var document = JsonDocument.Parse(circuitConfigJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("CircuitConfig must be a JSON object.");
            }

            if (document.RootElement.TryGetProperty("parts", out var parts) &&
                parts.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException("CircuitConfig.parts must be an array.");
            }
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"CircuitConfig is invalid JSON: {ex.Message}");
        }
    }

    private static string? ExtractProjectIdFromUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            if (!uri.Host.EndsWith("wokwi.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("WokwiProjectUrl must be a wokwi.com URL.");
            }

            var uriMatch = ProjectUrlRegex.Match(uri.AbsolutePath);
            return uriMatch.Success ? uriMatch.Groups["id"].Value : null;
        }

        var match = ProjectUrlRegex.Match(url);
        return match.Success ? match.Groups["id"].Value : null;
    }

    private async Task<ValidateWokwiProjectResponse> ProbeWokwiProjectAsync(
        string projectId,
        string projectUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, projectUrl);
            using var headResponse = await _httpClient.SendAsync(
                headRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (headResponse.IsSuccessStatusCode)
            {
                return BuildValidationResponse(projectId, projectUrl, headResponse.StatusCode, isSuccess: true);
            }

            if (headResponse.StatusCode == HttpStatusCode.MethodNotAllowed ||
                headResponse.StatusCode == HttpStatusCode.Forbidden ||
                headResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return await ProbeWokwiProjectWithGetAsync(projectId, projectUrl, cancellationToken);
            }

            return await ProbeWokwiProjectWithGetAsync(projectId, projectUrl, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new ValidateWokwiProjectResponse
            {
                IsValid = false,
                Message = PrivateOrMissingProjectMessage,
                WokwiProjectId = projectId,
                WokwiProjectUrl = projectUrl
            };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ValidateWokwiProjectResponse
            {
                IsValid = false,
                Message = "Không thể kiểm tra Wokwi project do request bị timeout.",
                WokwiProjectId = projectId,
                WokwiProjectUrl = projectUrl
            };
        }
    }

    private async Task<ValidateWokwiProjectResponse> ProbeWokwiProjectWithGetAsync(
        string projectId,
        string projectUrl,
        CancellationToken cancellationToken)
    {
        using var getRequest = new HttpRequestMessage(HttpMethod.Get, projectUrl);
        using var getResponse = await _httpClient.SendAsync(
            getRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        return BuildValidationResponse(projectId, projectUrl, getResponse.StatusCode, getResponse.IsSuccessStatusCode);
    }

    private static ValidateWokwiProjectResponse BuildValidationResponse(
        string projectId,
        string projectUrl,
        HttpStatusCode statusCode,
        bool isSuccess)
    {
        return new ValidateWokwiProjectResponse
        {
            IsValid = isSuccess,
            Message = isSuccess ? "Wokwi project hợp lệ." : PrivateOrMissingProjectMessage,
            WokwiProjectId = projectId,
            WokwiProjectUrl = projectUrl,
            StatusCode = (int)statusCode
        };
    }

    private void SyncClassAssignments(Lab lab, IReadOnlyCollection<int> classIds, int? scheduleId)
    {
        var now = DateTime.UtcNow;
        var targetIds = classIds.ToHashSet();
        var removed = lab.ClassAssignments
            .Where(assignment => !targetIds.Contains(assignment.ClassId))
            .ToList();

        _context.LabClassAssignments.RemoveRange(removed);

        var existingIds = lab.ClassAssignments
            .Where(assignment => targetIds.Contains(assignment.ClassId))
            .Select(assignment => assignment.ClassId)
            .ToHashSet();

        foreach (var classId in targetIds.Except(existingIds))
        {
            lab.ClassAssignments.Add(new LabClassAssignment
            {
                Id = Guid.NewGuid(),
                LabId = lab.Id,
                ClassId = classId,
                ScheduleId = scheduleId,
                CreatedAt = now
            });
        }

        // Update existing assignments with new scheduleId
        foreach (var assignment in lab.ClassAssignments.Where(a => targetIds.Contains(a.ClassId)))
        {
            assignment.ScheduleId = scheduleId;
        }
    }

    private static LabResponse MapLab(Lab lab)
    {
        var classes = lab.ClassAssignments
            .Where(assignment => assignment.Class != null)
            .OrderBy(assignment => assignment.Class!.ClassCode)
            .Select(assignment => new LabClassResponse
            {
                Id = assignment.Class!.Id,
                ClassCode = assignment.Class.ClassCode,
                CourseId = assignment.Class.CourseId,
                CourseTitle = assignment.Class.Course?.Title ?? string.Empty,
                SchoolId = assignment.Class.SchoolId,
                TeacherId = assignment.Class.TeacherId
            })
            .ToList();

        return new LabResponse
        {
            Id = lab.Id,
            Title = lab.Title,
            Description = lab.Description,
            Category = lab.Category,
            ThumbnailUrl = lab.ThumbnailUrl,
            SimulationMode = lab.SimulationMode,
            BoardType = lab.BoardType,
            StarterCode = lab.StarterCode,
            CircuitConfig = ParseJson(lab.CircuitConfigJson, "{}"),
            AllowedComponentTypes = ParseStringArray(lab.AllowedComponentTypesJson),
            WokwiProjectId = lab.WokwiProjectId,
            WokwiProjectUrl = lab.WokwiProjectId == null
                ? null
                : lab.WokwiProjectUrl ?? BuildWokwiProjectUrl(lab.WokwiProjectId),
            WokwiEmbedUrl = lab.WokwiProjectId == null ? null : BuildWokwiEmbedUrl(lab.WokwiProjectId),
            CreatedById = lab.CreatedById,
            CreatedByName = lab.CreatedBy?.FullName ?? string.Empty,
            Classes = classes,
            ClassIds = classes.Select(item => item.Id).ToList(),
            Status = lab.Status,
            LinkedAssignmentId = lab.LinkedAssignmentId,
            LinkedAssignmentTitle = lab.LinkedAssignment?.Title,
            Stats = BuildStats(lab),
            CreatedAt = lab.CreatedAt,
            UpdatedAt = lab.UpdatedAt
        };
    }

    private static LabProgressResponse MapProgress(LabProgress progress)
    {
        return new LabProgressResponse
        {
            LabId = progress.LabId,
            StudentId = progress.StudentId,
            StartedAt = progress.StartedAt,
            LastOpenedAt = progress.LastOpenedAt,
            OpenCount = progress.OpenCount,
            CompletedAt = progress.CompletedAt,
            DurationSeconds = progress.DurationSeconds
        };
    }

    private static LabStatsResponse BuildStats(Lab lab)
    {
        var progress = lab.Progresses.ToList();
        var targetStudentCount = lab.ClassAssignments
            .SelectMany(assignment => assignment.Class?.Enrollments ?? Array.Empty<Enrollment>())
            .Select(enrollment => enrollment.StudentId)
            .Distinct()
            .Count();

        var startedCount = progress.Select(item => item.StudentId).Distinct().Count();
        var completedCount = progress
            .Where(item => item.CompletedAt.HasValue)
            .Select(item => item.StudentId)
            .Distinct()
            .Count();
        var durations = progress
            .Where(item => item.CompletedAt.HasValue && item.DurationSeconds.HasValue)
            .Select(item => item.DurationSeconds!.Value)
            .ToList();

        return new LabStatsResponse
        {
            LabId = lab.Id,
            StudentCount = targetStudentCount > 0 ? targetStudentCount : startedCount,
            StartedCount = startedCount,
            CompletedCount = completedCount,
            CompletionRate = startedCount == 0 ? 0 : Math.Round((double)completedCount / startedCount, 4),
            AverageDurationSeconds = durations.Count == 0 ? null : Math.Round(durations.Average(), 2),
            CompletionSource = lab.LinkedAssignmentId.HasValue ? "linked_assignment" : "manual"
        };
    }

    private static JsonElement ParseJson(string? json, string fallback)
    {
        return JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(json) ? fallback : json);
    }

    private static IReadOnlyCollection<string> ParseStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyCollection<string>>(json) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private async Task<LabStatsResponse> BuildStatsAsync(Lab lab, CancellationToken cancellationToken)
    {
        var stats = BuildStats(lab);
        if (!lab.LinkedAssignmentId.HasValue)
        {
            return stats;
        }

        var completedCount = await _context.Submissions
            .AsNoTracking()
            .Where(submission =>
                submission.AssignmentId == lab.LinkedAssignmentId.Value &&
                submission.StudentId.HasValue &&
                (submission.Status == SubmissionStatuses.Submitted || submission.Status == SubmissionStatuses.Graded))
            .Select(submission => submission.StudentId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);

        var denominator = stats.StudentCount > 0 ? stats.StudentCount : stats.StartedCount;
        stats.CompletedCount = completedCount;
        stats.CompletionRate = denominator == 0 ? 0 : Math.Round((double)completedCount / denominator, 4);
        stats.CompletionSource = "linked_assignment";
        return stats;
    }

    private static string BuildWokwiProjectUrl(string projectId)
    {
        return $"https://wokwi.com/projects/{projectId}";
    }

    private static string BuildWokwiEmbedUrl(string projectId)
    {
        return $"{BuildWokwiProjectUrl(projectId)}?embed=1";
    }

    private sealed record LabPayload(
        string Title,
        string Description,
        string Category,
        string ThumbnailUrl,
        string SimulationMode,
        string BoardType,
        string? StarterCode,
        string CircuitConfigJson,
        string AllowedComponentTypesJson,
        IReadOnlyCollection<string> AllowedComponentTypes,
        string? WokwiProjectId,
        string? WokwiProjectUrl,
        IReadOnlyCollection<int> ClassIds,
        string Status,
        int? LinkedAssignmentId,
        int? ScheduleId);
}
