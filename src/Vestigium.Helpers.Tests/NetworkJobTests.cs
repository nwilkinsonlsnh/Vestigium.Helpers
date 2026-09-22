using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkJobTests
{
    [Fact]
    public async Task RunAsync_reports_progress_and_result()
    {
        var job = new NetworkJob<int>("job-1", "probe", async (token, progress) =>
        {
            progress?.Report(new NetworkProgress { JobId = "job-1", Phase = "Run", Sent = 1, Received = 1, Sequence = 1, LastStatus = "ok", LastRoundtripMs = 3 });
            await Task.Yield();
            token.ThrowIfCancellationRequested();
            return 7;
        });

        NetworkProgress? seen = null;
        job.ProgressChanged += (_, p) => seen = p;
        var result = await job.RunAsync();
        Assert.Equal(7, result);
        Assert.Equal("Run", job.Progress.Phase);
        Assert.Equal(1, job.Progress.Sent);
        Assert.Equal("ok", job.Progress.LastStatus);
        Assert.NotNull(seen);
        await Assert.ThrowsAsync<InvalidOperationException>(() => job.RunAsync());
    }

    [Fact]
    public async Task Cancel_before_run_cancels()
    {
        var job = new NetworkJob<int>("job-2", "probe", (token, _) =>
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(1);
        });
        job.Cancel();
        job.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => job.RunAsync());
    }

    [Fact]
    public async Task Caller_token_cancels()
    {
        var job = new NetworkJob<int>("job-3", "probe", async (token, _) =>
        {
            await Task.Delay(30_000, token);
            return 1;
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => job.RunAsync(cts.Token));
    }
}
