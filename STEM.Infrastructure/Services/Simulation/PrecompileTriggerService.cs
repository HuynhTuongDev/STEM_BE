using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using STEM.Application.UseCases.Simulation.Abstractions;

namespace STEM.Infrastructure.Services.Simulation;

// Singleton — giữ IServiceScopeFactory để tự tạo scope MỚI cho mỗi lần chạy
// nền (IFirmwareCacheService/ISimulationCompileService phụ thuộc StemDbContext,
// Scoped) — không dùng scope của request HTTP gốc, vì request đó (Publish
// Lab, hoặc debounce-precompile lúc gõ code) đã trả response từ lâu trước
// khi compile ~40-90s thật sự xong.
public sealed class PrecompileTriggerService : IPrecompileTriggerService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrecompileTriggerService> _logger;

    public PrecompileTriggerService(IServiceScopeFactory scopeFactory, ILogger<PrecompileTriggerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void TriggerBackgroundCompile(string sourceCode, string board, string framework, Guid? buildCacheScopeId)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var firmwareCache = scope.ServiceProvider.GetRequiredService<IFirmwareCacheService>();

                // CompileAndCacheAsync tự kiểm tra cache trước khi compile thật
                // (xem FirmwareCacheService) — gọi thẳng ở đây an toàn, không lo
                // tốn công compile lại nếu nội dung đã có sẵn trong cache.
                var result = await firmwareCache.CompileAndCacheAsync(
                    sourceCode,
                    board,
                    framework,
                    buildCacheScopeId,
                    CancellationToken.None);

                if (!result.Success)
                {
                    _logger.LogWarning(
                        "Precompile nền thất bại (scope={ScopeId}): {Errors}",
                        buildCacheScopeId,
                        string.Join("; ", result.Errors.Select(e => e.Message)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Precompile nền gặp lỗi hệ thống (scope={ScopeId})", buildCacheScopeId);
            }
        });
    }
}
