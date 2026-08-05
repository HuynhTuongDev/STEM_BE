namespace STEM.Application.UseCases.Simulation.Abstractions;

// Kích hoạt 1 lần compile-và-lưu-cache CHẠY NỀN (fire-and-forget) — dùng
// chung cho mọi nơi muốn "làm ấm" IFirmwareCacheService trước khi học sinh
// thực sự bấm Run: precompile starterCode lúc giáo viên Publish/sửa Lab,
// và precompile khi học sinh đang gõ code (debounce ở FE). Không throw ra
// caller — lỗi ở đây chỉ là mất cơ hội tối ưu tốc độ, Run vẫn hoạt động
// đúng (sẽ tự compile khi cache miss, như trước khi có cơ chế này).
public interface IPrecompileTriggerService
{
    void TriggerBackgroundCompile(string sourceCode, string board, string framework, Guid? buildCacheScopeId);
}
