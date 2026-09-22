using Vestigium.Helpers;
using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Processes;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessDeltaTests
{
    [Fact]
    public void First_sample_cpu_usage_is_not_a_hit()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("CPU.Usage GT 0", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());

        var first = new ProcessInfo { Pid = 9, Name = "app.exe", CpuTime = TimeSpan.FromSeconds(1) };
        var extras = ProcessDeltaMap.Diff(first, prior: null, TimeSpan.FromSeconds(1));
        Assert.False(compiled.Query!.Matches(new ProcessKqlRow(first, extras)));
    }

    [Fact]
    public void Second_sample_with_more_cpu_time_hits_usage()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("CPU.Usage GT 0", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());

        var first = new ProcessInfo { Pid = 9, Name = "app.exe", CpuTime = TimeSpan.FromSeconds(1) };
        var second = new ProcessInfo { Pid = 9, Name = "app.exe", CpuTime = TimeSpan.FromSeconds(2) };
        var extras = ProcessDeltaMap.Diff(second, first, TimeSpan.FromSeconds(1));
        Assert.True(extras.ContainsKey("CPU.Usage"));
        Assert.True(compiled.Query!.Matches(new ProcessKqlRow(second, extras)));
    }

    [Fact]
    public void Watch_tick_fault_logs_warning_without_query_text()
    {
        HelperLog.Shutdown();
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(HelperLog.AppIds.Processes, cfg => cfg.LogDirectory = dir);
        try
        {
            using var watch = ProcessHelper.Watch("Name LIKE '%edge%'", TimeSpan.FromMilliseconds(250), ProcessWatchFields.All);
            var gotEmpty = new ManualResetEventSlim(false);
            watch.Sampled += (_, sample) =>
            {
                if (sample.Matches.Count == 0)
                    gotEmpty.Set();
            };
            ProcessTestHooks.QueryWatchFault = new InvalidOperationException("boom CCleaner% secret");
            Assert.True(gotEmpty.Wait(TimeSpan.FromSeconds(5)));
            Assert.Contains(
                HelperLog.RecentJsonLines,
                line => line.Contains("Watch query tick failed", StringComparison.Ordinal));
            Assert.DoesNotContain(
                HelperLog.RecentJsonLines,
                line => line.Contains("CCleaner", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("%edge%", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            ProcessTestHooks.QueryWatchFault = null;
            HelperLog.Shutdown();
        }
    }
}
