using STEM.Application.UseCases.Simulation.Runtime;

namespace STEM.Application.Tests;

public sealed class RunningSimulationRegistryTests
{
    [Fact]
    public void TryCancel_CancelsRegisteredToken()
    {
        var registry = new RunningSimulationRegistry();
        using var cts = new CancellationTokenSource();
        registry.Register("proj-1", cts);

        var cancelled = registry.TryCancel("proj-1");

        Assert.True(cancelled);
        Assert.True(cts.IsCancellationRequested);
    }

    [Fact]
    public void TryCancel_ReturnsFalse_WhenNothingRegistered()
    {
        var registry = new RunningSimulationRegistry();

        var cancelled = registry.TryCancel("no-such-project");

        Assert.False(cancelled);
    }

    [Fact]
    public void TryCancel_ReturnsFalse_AfterRemove()
    {
        var registry = new RunningSimulationRegistry();
        var cts = new CancellationTokenSource();
        registry.Register("proj-1", cts);
        registry.Remove("proj-1");

        var cancelled = registry.TryCancel("proj-1");

        Assert.False(cancelled);
    }

    [Fact]
    public void Register_CancelsStaleTokenInstead_OfLeakingIt_WhenCalledTwiceForSameProject()
    {
        var registry = new RunningSimulationRegistry();
        var firstRun = new CancellationTokenSource();
        var secondRun = new CancellationTokenSource();

        registry.Register("proj-1", firstRun);
        registry.Register("proj-1", secondRun);

        // Lần chạy cũ phải bị hủy ngay khi bị thay thế, không mồ côi.
        Assert.True(firstRun.IsCancellationRequested);
        Assert.False(secondRun.IsCancellationRequested);

        var cancelled = registry.TryCancel("proj-1");
        Assert.True(cancelled);
        Assert.True(secondRun.IsCancellationRequested);
    }

    // Bước 7: IsRunning là kiểm tra KHÔNG phá hủy trạng thái — khác TryCancel
    // (có side-effect hủy). Dùng để chặn Submit mà không vô tình dừng luôn
    // lần chạy đang diễn ra chỉ vì kiểm tra nó.
    [Fact]
    public void IsRunning_ReturnsTrue_WhileRegistered_FalseAfterRemove()
    {
        var registry = new RunningSimulationRegistry();
        using var cts = new CancellationTokenSource();

        Assert.False(registry.IsRunning("proj-1"));

        registry.Register("proj-1", cts);
        Assert.True(registry.IsRunning("proj-1"));
        Assert.False(cts.IsCancellationRequested, "IsRunning không được có side-effect hủy CTS.");

        registry.Remove("proj-1");
        Assert.False(registry.IsRunning("proj-1"));
    }
}
