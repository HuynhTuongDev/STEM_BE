using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Abstractions;

// Cache firmware ESP32 đã compile, khoá theo NỘI DUNG (sourceCode + board +
// framework + fqbn đã resolve + phiên bản instrumentation GPIO), KHÔNG theo
// project — 2 học sinh khác nhau (hoặc cùng 1 học sinh chạy lại) dùng đúng
// cùng 1 sourceCode thì dùng chung được 1 firmware đã compile sẵn, không cần
// compile lại. Bổ trợ (không thay thế) cache build-path theo từng project đã
// có ở SimulationCompileService — cache đó tăng tốc CHÍNH lần compile thật
// (~86% thời gian compile là core framework, tái dùng object file), còn cache
// này né được HẲN việc phải compile khi nội dung đã từng compile trước đó.
//
// GPIO instrumentation (#define digitalWrite injection) sống ở đây — dùng
// chung cho cả compile thật lúc học sinh Run (QemuEsp32Runner) lẫn precompile
// lúc giáo viên lưu bài mẫu (LabService) — đảm bảo cache key nhất quán tuyệt
// đối giữa 2 nơi gọi.
public interface IFirmwareCacheService
{
    // Chỉ đọc cache, không compile — dùng cho đường Run thật (QemuEsp32Runner)
    // để quyết định có cần phát StudentCompileStarted/Finished hay bỏ qua hẳn
    // giai đoạn "Đang biên dịch..." khi đã có sẵn firmware.
    Task<CompileSimulationResponse?> TryGetCachedFirmwareAsync(
        string sourceCode,
        string board,
        string framework,
        CancellationToken cancellationToken);

    // Compile thật (qua ISimulationCompileService) rồi lưu cache nếu thành
    // công. `buildCacheScopeId` (tuỳ chọn) truyền tiếp làm
    // CompileSimulationRequest.ProjectId — cho phép compile thật bên trong
    // vẫn hưởng cache build-path theo scope (project thật lúc Run, hoặc LabId
    // lúc precompile bài mẫu) dù đây là cache firmware theo nội dung, không
    // theo scope.
    Task<CompileSimulationResponse> CompileAndCacheAsync(
        string sourceCode,
        string board,
        string framework,
        Guid? buildCacheScopeId,
        CancellationToken cancellationToken);
}
