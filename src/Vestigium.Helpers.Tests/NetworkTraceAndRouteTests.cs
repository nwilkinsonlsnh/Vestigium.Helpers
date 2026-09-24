using Vestigium.Helpers.Network;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class NetworkTraceAndRouteTests
{
    public NetworkTraceAndRouteTests()
    {
        VestigiumLogger.Shutdown();
    }

    [Fact]
    public void Trace_engine_finish_calls_logfinished()
    {
        var path = FindNetworkSource("IcmpTraceEngine.cs");
        Assert.True(path is not null, "IcmpTraceEngine.cs not found walking up from BaseDirectory.");
        var src = File.ReadAllText(path);
        Assert.Contains("IcmpEchoEngine.LogFinished(", src, StringComparison.Ordinal);
        Assert.DoesNotContain("NetworkLog.Success(", src, StringComparison.Ordinal);
    }

    [Fact]
    public void LogFinished_failed_and_timedout_use_operation_failed()
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
    public void LogFinished_cancelled_uses_operation_warning()
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
    public void Trace_cancelled_beats_reached()
    {
        var hop = new IcmpTraceHop(1, "127.0.0.1", []);
        Assert.Equal(NetworkJobStatus.Cancelled, IcmpTraceEngine.DecideStatus(true, true, [hop]));
        Assert.Equal(NetworkJobStatus.Success, IcmpTraceEngine.DecideStatus(false, true, [hop]));
        Assert.Equal(NetworkJobStatus.TimedOut, IcmpTraceEngine.DecideStatus(false, false, []));
    }

    [Fact]
    public void Win_ipv6_change_and_remove_log_success()
    {
        var path = FindNetworkSource("NetworkRouteMutation.cs");
        Assert.True(path is not null, "NetworkRouteMutation.cs not found walking up from BaseDirectory.");
        var src = File.ReadAllText(path);
        Assert.Contains(
            "NetworkLog.Success(HelperLog.Subcategories.Route, $\"AddRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway} win-v6\")",
            src,
            StringComparison.Ordinal);
        Assert.Contains(
            "NetworkLog.Success(HelperLog.Subcategories.Route, $\"ChangeRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway} win-v6\")",
            src,
            StringComparison.Ordinal);
        Assert.Contains(
            "NetworkLog.Success(HelperLog.Subcategories.Route, $\"RemoveRoute dest={change.Destination}/{change.PrefixLength} gw={change.Gateway} win-v6\")",
            src,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Ipv6_write_does_not_borrow_ipv4_interface_index()
    {
        var path = FindNetworkSource("NetworkRouteMutation.cs");
        Assert.True(path is not null, "NetworkRouteMutation.cs not found walking up from BaseDirectory.");
        var src = File.ReadAllText(path);
        Assert.Contains("TryFirstIpv6Index()", src, StringComparison.Ordinal);
        Assert.Contains("BindV6Index(", src, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "ResolveInterfaceIndex(change.InterfaceIndex, TryFirstIpv4Index())",
            src,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_ipv6_interface_does_not_use_index_1()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            NetworkRouteMutation.ResolveInterfaceIndex(null, null, "IPv6"));
        Assert.Contains("InterfaceIndex", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IPv6", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("IPv4", ex.Message, StringComparison.Ordinal);
        Assert.Equal(8, NetworkRouteMutation.ResolveInterfaceIndex(8, null, "IPv6"));
        Assert.Equal(3, NetworkRouteMutation.ResolveInterfaceIndex(null, 3, "IPv6"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkRouteMutation.ResolveInterfaceIndex(0, 1, "IPv6"));
    }

    [Fact]
    public void First_ipv6_index_is_null_or_at_least_one()
    {
        var index = NetworkRouteMutation.TryFirstIpv6Index();
        if (index is { } found)
            Assert.True(found >= 1, found.ToString());
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
