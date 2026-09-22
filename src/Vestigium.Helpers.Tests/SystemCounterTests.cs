using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class SystemCounterTests
{
    [Fact]
    public void GetSystemCounters_fills_kernel_commit_and_topology()
    {
        var snap = ProcessHelper.GetSystemCounters();
        Assert.True(snap.PhysicalTotal > 0);
        Assert.True(snap.CommitLimit > 0);
        Assert.True(snap.KernelWorkingSet > 0 || snap.CacheWorkingSet > 0 || snap.Nonpaged > 0);
        Assert.True(snap.Cores >= 1);
        Assert.True(snap.Sockets >= 1);
        Assert.True(snap.LogicalProcessors >= 1);
        Assert.NotNull(snap.ReadOperations);
        Assert.NotNull(snap.WriteOperations);
        Assert.Null(snap.CpuPercent);
        Assert.Null(snap.ReadDelta);
        Assert.NotNull(snap.GpuAdapters);
    }

    [Fact]
    public void GetSystemCounters_paging_lists_are_bytes_or_unsupported()
    {
        var snap = ProcessHelper.GetSystemCounters();
        var lists = new long?[] { snap.Zeroed, snap.Free, snap.Modified, snap.Standby, snap.Priority0 };
        Assert.True(lists.Any(v => v is > 0) || lists.All(v => v is null or 0));
    }

    [Fact]
    public async Task WatchSystem_second_sample_has_io_or_page_delta()
    {
        var samples = new List<SystemCounters>();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var watch = ProcessHelper.WatchSystem(TimeSpan.FromMilliseconds(250));
        watch.Sampled += (_, sample) =>
        {
            lock (samples)
            {
                samples.Add(sample);
                if (samples.Count >= 2)
                    ready.TrySetResult();
            }
        };
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(8));
        lock (samples)
        {
            Assert.Null(samples[0].ReadDelta);
            Assert.True(
                samples[1].ReadDelta is not null
                || samples[1].PageFaultDelta is not null
                || samples[1].ContextSwitchDelta is not null);
        }
    }
}
