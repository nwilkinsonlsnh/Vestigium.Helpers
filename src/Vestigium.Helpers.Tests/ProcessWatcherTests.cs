using System.Diagnostics;
using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessWatcherTests
{
    [Fact]
    public void Watch_rejects_interval_out_of_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProcessHelper.Watch(Environment.ProcessId, TimeSpan.FromMilliseconds(249), ProcessWatchFields.Cpu));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProcessHelper.Watch(Environment.ProcessId, TimeSpan.FromSeconds(61), ProcessWatchFields.Cpu));
    }

    [Fact]
    public void Watch_rejects_non_positive_pid()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProcessHelper.Watch(0, TimeSpan.FromMilliseconds(250), ProcessWatchFields.Cpu));

    [Fact]
    public async Task Watch_two_samples_at_250ms_have_cpu_or_time_delta()
    {
        var samples = new List<ProcessSample>();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var watch = ProcessHelper.Watch(
            Environment.ProcessId,
            TimeSpan.FromMilliseconds(250),
            ProcessWatchFields.Cpu | ProcessWatchFields.PrivateBytes);
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
        ProcessSample first, second;
        lock (samples)
        {
            first = samples[0];
            second = samples[1];
        }

        Assert.Null(first.CpuPercent);
        Assert.Null(first.CpuTimeDelta);
        Assert.True(second.CpuPercent is not null || second.CpuTimeDelta is not null);
    }

    [Fact]
    public async Task WatchSystem_second_sample_has_cpu_percent()
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
            Assert.Null(samples[0].CpuPercent);
            Assert.NotNull(samples[1].CpuPercent);
            Assert.True(samples[1].PhysicalTotal > 0);
            Assert.True(samples[1].LogicalProcessors >= 1);
        }
    }

    [Fact]
    public async Task Watch_raises_exited_when_child_dies()
    {
        using var child = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "ping.exe"),
            Arguments = "-n 30 127.0.0.1",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        Assert.NotNull(child);
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            using var watch = ProcessHelper.Watch(child.Id, TimeSpan.FromMilliseconds(250), ProcessWatchFields.Cpu);
            watch.Exited += (_, _) => exited.TrySetResult();
            child.Kill(entireProcessTree: true);
            await exited.Task.WaitAsync(TimeSpan.FromSeconds(8));
        }
        finally
        {
            try { if (!child.HasExited) child.Kill(entireProcessTree: true); } catch { }
        }
    }

    [Fact]
    public void GetSystemCounters_has_physical_memory_and_logical_processors()
    {
        var snap = ProcessHelper.GetSystemCounters();
        Assert.True(snap.PhysicalTotal > 0);
        Assert.True(snap.LogicalProcessors >= 1);
        Assert.Null(snap.CpuPercent);
    }
}
