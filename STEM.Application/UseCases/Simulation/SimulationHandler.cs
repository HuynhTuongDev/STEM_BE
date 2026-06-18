using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Simulations;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Simulation;

/// <summary>
/// Handler tổng hợp cho module Simulation (Wokwi Lab).
/// Chứa toàn bộ business logic: tạo/cập nhật template, tạo session,
/// sinh Python code và liệt kê component catalog.
/// </summary>
public class SimulationHandler
{
    private readonly ISimulationRepository        _simulationRepository;
    private readonly ISimulationSessionRepository _sessionRepository;
    private readonly IWokwiService                _wokwiService;

    public SimulationHandler(
        ISimulationRepository        simulationRepository,
        ISimulationSessionRepository sessionRepository,
        IWokwiService                wokwiService)
    {
        _simulationRepository = simulationRepository;
        _sessionRepository    = sessionRepository;
        _wokwiService         = wokwiService;
    }

    // ── Template ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Tạo SimulationTemplate mới. Validate diagram.json trước khi lưu;
    /// ném <see cref="InvalidOperationException"/> nếu diagram không hợp lệ.
    /// </summary>
    public async Task<TemplateResponse> CreateTemplateAsync(
        CreateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Parse + Validate diagram
        var diagram = _wokwiService.ParseDiagram(request.DiagramJson);
        var (isValid, errors) = _wokwiService.ValidateDiagram(diagram);
        if (!isValid)
        {
            throw new InvalidOperationException(
                $"Diagram không hợp lệ: {string.Join("; ", errors)}");
        }

        // 2. Tạo entity
        var template = new SimulationTemplate
        {
            SimulationName = request.SimulationName,
            Description    = request.Description,
            Config         = request.DiagramJson,
            // SimulationId = 0: placeholder — cần wiring với SimulationEntity
            // khi tích hợp đầy đủ với Lesson/Course flow.
            SimulationId   = 0
        };

        await _simulationRepository.AddAsync(template, cancellationToken);
        await _simulationRepository.SaveChangesAsync(cancellationToken);

        return MapToTemplateResponse(template);
    }

    /// <summary>
    /// Cập nhật diagram.json của template đã có.
    /// Validate trước khi ghi đè — ném ngoại lệ nếu template không tồn tại
    /// hoặc diagram mới không hợp lệ.
    /// </summary>
    public async Task<TemplateResponse> UpdateDiagramAsync(
        int                   templateId,
        UpdateDiagramRequest  request,
        CancellationToken     cancellationToken = default)
    {
        var template = await _simulationRepository.GetByIdAsync(templateId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy template với Id = {templateId}.");

        var diagram = _wokwiService.ParseDiagram(request.DiagramJson);
        var (isValid, errors) = _wokwiService.ValidateDiagram(diagram);
        if (!isValid)
        {
            throw new InvalidOperationException(
                $"Diagram không hợp lệ: {string.Join("; ", errors)}");
        }

        template.Config    = request.DiagramJson;
        template.UpdatedAt = DateTime.UtcNow;

        _simulationRepository.Update(template);
        await _simulationRepository.SaveChangesAsync(cancellationToken);

        return MapToTemplateResponse(template);
    }

    /// <summary>
    /// Lấy thông tin template theo Id.
    /// Ném <see cref="KeyNotFoundException"/> nếu không tồn tại.
    /// </summary>
    public async Task<TemplateResponse> GetTemplateAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var template = await _simulationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy template với Id = {id}.");

        return MapToTemplateResponse(template);
    }

    /// <summary>
    /// Sinh Python code mẫu từ Config của template (mock GPIO, không cần hardware thật).
    /// </summary>
    public async Task<string> GetPythonCodeAsync(
        int               templateId,
        CancellationToken cancellationToken = default)
    {
        var template = await _simulationRepository.GetByIdAsync(templateId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy template với Id = {templateId}.");

        var diagram = _wokwiService.ParseDiagram(template.Config);
        return _wokwiService.GeneratePythonCode(diagram);
    }

    // ── Session ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Tạo một phiên lab mới cho student trên một template cụ thể.
    /// </summary>
    public async Task<SessionResponse> CreateSessionAsync(
        CreateSessionRequest request,
        CancellationToken    cancellationToken = default)
    {
        // Đảm bảo template tồn tại trước khi tạo session
        var templateExists = await _simulationRepository.ExistsAsync(request.TemplateId, cancellationToken);
        if (!templateExists)
        {
            throw new KeyNotFoundException($"Template Id = {request.TemplateId} không tồn tại.");
        }

        var session = new SimulationSession
        {
            StudentId  = request.StudentId,
            TemplateId = request.TemplateId,
            StartTime  = DateTime.UtcNow,
            Status     = "Active"
        };

        await _sessionRepository.AddAsync(session, cancellationToken);
        await _sessionRepository.SaveChangesAsync(cancellationToken);

        return MapToSessionResponse(session);
    }

    // ── Component Catalog ─────────────────────────────────────────────────────

    /// <summary>
    /// Trả về danh sách tất cả component được hỗ trợ trong sandbox.
    /// Đây là operation đồng bộ vì catalog được load từ bộ nhớ.
    /// </summary>
    public IEnumerable<ComponentMetaResponse> GetComponents()
    {
        return _wokwiService.GetAllComponents().Select(c => new ComponentMetaResponse
        {
            Type        = c.Type,
            Label       = c.Label,
            Description = c.Description,
            Pins        = c.Pins,
            GpioPins    = c.GpioPins,
            DefaultAttrs = c.DefaultAttrs
        });
    }

    // ── Private mappers ───────────────────────────────────────────────────────

    private static TemplateResponse MapToTemplateResponse(SimulationTemplate t) =>
        new TemplateResponse
        {
            Id             = t.Id,
            SimulationName = t.SimulationName,
            Description    = t.Description,
            DiagramJson    = t.Config,
            CreatedAt      = t.CreatedAt
        };

    private static SessionResponse MapToSessionResponse(SimulationSession s) =>
        new SessionResponse
        {
            Id         = s.Id,
            StudentId  = s.StudentId,
            TemplateId = s.TemplateId,
            StartTime  = s.StartTime,
            Status     = s.Status
        };
}
