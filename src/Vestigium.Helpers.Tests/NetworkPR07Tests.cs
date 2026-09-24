using Vestigium.Helpers.Network;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class NetworkPr07Tests
{
    public NetworkPr07Tests()
    {
        VestigiumLogger.Shutdown();
    }

    [Fact]
    public void PR07_01_trace_finish_uses_logfinished()
    {
        var path = FindNetworkSource("IcmpTraceEngine.cs");
        Assert.True(path is not null, "IcmpTraceEngine.cs not found walking up from BaseDirectory.");
        var src = File.ReadAllText(path);
        Assert.Contains("IcmpEchoEngine.LogFinished(", src, StringComparison.Ordinal);
        Assert.DoesNotContain("NetworkLog.Success(", src, StringComparison.Ordinal);
    }

    [Fact]
    public void PR07_01_logfinished_failed_is_not_complete()
    {
        try
        {
            Init();
            IcmpEchoEngine.LogFinished(NetworkJobStatus.Failed, "Failed trace job=trace-x");
            IcmpEchoEngine.LogFinished(NetworkJobStatus.TimedOut, "TimedOut trace job=trace-y");
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line =>
                line.Contains("\"EVENTID\":14520") && line.Contains("Failed trace"));
            Assert.Contains(VestigiumLogger.RecentJsonLines, line =>
                line.Contains("\"EVENTID\":14520") && line.Contains("TimedOut trace"));
            Assert.DoesNotContain(VestigiumLogger.RecentJsonLines, line =>
                line.Contains("\"EVENTID\":14515") && line.Contains("Failed trace"));
            Assert.DoesNotContain(VestigiumLogger.RecentJsonLines, line =>
                line.Contains("\"EVENTID\":14515") && line.Contains("TimedOut trace"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void PR07_01_logfinished_cancelled_is_warning()
    {
        try
        {
            Init();
            IcmpEchoEngine.LogFinished(NetworkJobStatus.Cancelled, "Cancelled trace job=trace-z");
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line =>
                line.Contains("\"EVENTID\":14525") && line.Contains("Cancelled trace"));
            Assert.DoesNotContain(VestigiumLogger.RecentJsonLines, line =>
                line.Contains("\"EVENTID\":14515") && line.Contains("Cancelled trace"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void PR07_01_decide_status_cancelled_beats_reached()
    {
        var hop = new IcmpTraceHop(1, "127.0.0.1", []);
        Assert.Equal(NetworkJobStatus.Cancelled, IcmpTraceEngine.DecideStatus(true, true, [hop]));
        Assert.Equal(NetworkJobStatus.Success, IcmpTraceEngine.DecideStatus(false, true, [hop]));
        Assert.Equal(NetworkJobStatus.TimedOut, IcmpTraceEngine.DecideStatus(false, false, []));
    }

    private static void Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumNetLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = NetworkCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            NetworkCatalog.Register(cfg);
        });
    }

    private static string? FindNetworkSource(string fileName)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var hit = Path.Combine(dir.FullName, "src", "Vestigium.Helpers.Network", fileName);
            if (File.Exists(hit))
                return hit;
        }

        return null;
    }
}
