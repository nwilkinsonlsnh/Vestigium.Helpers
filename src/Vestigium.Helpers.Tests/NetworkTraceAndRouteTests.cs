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

    [Fact]
    public void Catalog_register_includes_share_stats_progress()
    {
        var path = FindNetworkSource("NetworkCatalog.cs");
        Assert.True(path is not null, "NetworkCatalog.cs not found walking up from BaseDirectory.");
        var src = File.ReadAllText(path);
        Assert.Contains("\"Share\"", src, StringComparison.Ordinal);
        Assert.Contains("\"Stats\"", src, StringComparison.Ordinal);
        Assert.Contains("\"Progress\"", src, StringComparison.Ordinal);
    }

    [Fact]
    public void NetworkHelper_comment_matches_option_c()
    {
        var path = FindNetworkSource("NetworkHelper.cs");
        Assert.True(path is not null, "NetworkHelper.cs not found walking up from BaseDirectory.");
        var src = File.ReadAllText(path);
        Assert.Contains("Linux IPv4 and IPv6 netlink", src, StringComparison.Ordinal);
        Assert.Contains("Option C", src, StringComparison.Ordinal);
        Assert.DoesNotContain("Linux writes throw typed denies", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Named_events_14530_14535_14540_exist()
    {
        Assert.Equal(14530, NetworkEvents.RouteDenied);
        Assert.Equal(14535, NetworkEvents.IcmpForbidden);
        Assert.Equal(14540, NetworkEvents.CampaignWindowMissed);
        Assert.Contains(NetworkCatalog.Rows, r => r.EventId == 14530 && r.Name == "RouteDenied");
        Assert.Contains(NetworkCatalog.Rows, r => r.EventId == 14535 && r.Name == "IcmpForbidden");
        Assert.Contains(NetworkCatalog.Rows, r => r.EventId == 14540 && r.Name == "CampaignWindowMissed");
        var jsonPath = FindNetworkSource(Path.Combine("EventCatalog", "network.json"));
        Assert.True(jsonPath is not null, "network.json not found.");
        var json = File.ReadAllText(jsonPath);
        Assert.Contains("14530", json, StringComparison.Ordinal);
        Assert.Contains("14535", json, StringComparison.Ordinal);
        Assert.Contains("14540", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Path_escape_dns_peer_and_oui_reject_use_named_events()
    {
        Assert.Equal(14545, NetworkEvents.CampaignPathEscape);
        Assert.Equal(14550, NetworkEvents.DnsPeerMismatch);
        Assert.Equal(14555, NetworkEvents.OuiLookupRejected);
        var root = Path.Combine(Path.GetTempPath(), "vest-camp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Init();
            NetworkTestHooks.CampaignRoot = root;
            Assert.Throws<ArgumentException>(() => CampaignPaths.Confine(Path.GetTempPath(), "recipe"));
            NetworkLog.DnsPeerMismatch("udp discarded foreign source=192.0.2.9:53", fatal: false);
            Assert.Throws<ArgumentException>(() =>
                OuiLookupGuard.Bind("http://api.macvendors.com/00-00-0C", new OuiLookupOptions()));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line =>
                line.Contains("\"EVENTID\":14545") && line.Contains("path escape"));
            Assert.Contains(VestigiumLogger.RecentJsonLines, line =>
                line.Contains("\"EVENTID\":14550") && line.Contains("foreign source"));
            Assert.Contains(VestigiumLogger.RecentJsonLines, line =>
                line.Contains("\"EVENTID\":14555") && line.Contains("scheme="));
        }
        finally
        {
            NetworkTestHooks.Reset();
            VestigiumLogger.Shutdown();
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public void Default_route_write_logs_route_denied()
    {
        try
        {
            Init();
            var ex = Assert.Throws<NetworkRouteDenied>(() => NetworkHelper.AddRoute(new NetworkRouteChange
            {
                Destination = "0.0.0.0",
                PrefixLength = 0,
                Gateway = "1.1.1.1",
                InterfaceIndex = 1
            }));
            Assert.Contains("Default route", ex.Message, StringComparison.OrdinalIgnoreCase);
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line =>
                line.Contains("\"EVENTID\":14530") && line.Contains("default route"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Icmp_forbidden_and_window_missed_use_named_events()
    {
        try
        {
            Init();
            NetworkLog.IcmpForbidden("Failed job=echo-x ICMP not permitted");
            NetworkLog.WindowMissed("missed campaign=c1 date=2026-09-24 time=02:00");
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line =>
                line.Contains("\"EVENTID\":14535") && line.Contains("ICMP not permitted"));
            Assert.Contains(VestigiumLogger.RecentJsonLines, line =>
                line.Contains("\"EVENTID\":14540") && line.Contains("missed campaign"));
            Assert.DoesNotContain(VestigiumLogger.RecentJsonLines, line =>
                line.Contains("\"EVENTID\":14515") && line.Contains("ICMP not permitted"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Package_version_is_1_2_0()
    {
        var path = FindNetworkSource("Vestigium.Helpers.Network.csproj");
        Assert.True(path is not null, "Network csproj not found walking up from BaseDirectory.");
        var src = File.ReadAllText(path);
        Assert.Contains("<Version>1.2.0</Version>", src, StringComparison.Ordinal);
        Assert.Contains("1.2.0 is PR10", src, StringComparison.Ordinal);
        Assert.DoesNotContain("<Version>1.0.0</Version>", src, StringComparison.Ordinal);
        Assert.DoesNotContain("<Version>1.0.1</Version>", src, StringComparison.Ordinal);
        Assert.DoesNotContain("<Version>1.1.0</Version>", src, StringComparison.Ordinal);
        Assert.Contains("Vestigium.Helpers.Json\" Version=\"1.0.1\"", src, StringComparison.Ordinal);
        Assert.Contains("Vestigium.Helpers.Analytics\" Version=\"1.0.1\"", src, StringComparison.Ordinal);
        Assert.Contains("Vestigium.Helpers.FileIo\" Version=\"1.1.1\"", src, StringComparison.Ordinal);
        Assert.DoesNotContain("Include=\"Vestigium.Logging\"", src, StringComparison.Ordinal);
        Assert.DoesNotContain("Include=\"Vestigium.Helpers.Charts\"", src, StringComparison.Ordinal);
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
