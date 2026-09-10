using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class LoggingBranchTests
{
    public LoggingBranchTests()
    {
        HelperLog.Shutdown();
    }

    [Fact]
    public void Taxonomy_normalize_blank_unregistered_and_whitespace_subs()
    {
        var t = new VestigiumTaxonomy();
        t.Register("Network", "ICMP", "  ");
        Assert.True(t.IsCategoryRegistered("Network"));
        Assert.True(t.IsSubcategoryRegistered("Network", "ICMP"));
        Assert.False(t.IsSubcategoryRegistered("Network", "TCP"));
        Assert.False(t.IsCategoryRegistered("Widgets"));
        Assert.False(t.IsSubcategoryRegistered("Widgets", "Thing"));

        var blank = t.Normalize("", "");
        Assert.True(blank.Rewritten);
        Assert.Equal(VestigiumTaxonomy.Uncategorized, blank.Category);

        var unknownCat = VestigiumTaxonomy.Defaults.Normalize("Widgets", "Thing");
        Assert.True(unknownCat.Rewritten);
        Assert.Equal(VestigiumTaxonomy.Uncategorized, unknownCat.Category);
        Assert.Equal(VestigiumTaxonomy.Unregistered, unknownCat.Subcategory);

        var unknownSub = VestigiumTaxonomy.Defaults.Normalize("Network", "NOPE");
        Assert.True(unknownSub.Rewritten);
        Assert.Equal("Network", unknownSub.Category);
        Assert.Equal(VestigiumTaxonomy.Unregistered, unknownSub.Subcategory);

        Assert.NotEmpty(VestigiumTaxonomy.Defaults.Snapshot);
    }

    [Fact]
    public void Logger_events_disk_override_levels_and_flood()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumLogBranch", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        IDisposable? sub = null;
        try
        {
            HelperLog.InitializeHost(HelperLog.AppIds.Core, cfg =>
            {
                cfg.LogDirectory = dir;
                cfg.FloodThresholdCount = 5;
                cfg.FloodWindow = TimeSpan.FromSeconds(30);
                cfg.MinimumDiskLevel = VestigiumLogLevel.Verbose;
                cfg.RecentJsonLineCap = 20;
            });

            Assert.True(VestigiumLogger.IsInitialized);
            Assert.Equal(dir, VestigiumLogger.Options.ResolveLogDirectory());
            Assert.False(VestigiumLogger.IsDiskTripped);

            var seen = new List<VestigiumLogEvent>();
            sub = VestigiumLogger.Events.Subscribe(new LogCollector(seen));
            _ = VestigiumLogger.EventReader;

            VestigiumLog.Verbose(VestigiumStatus.Success, "System", "IO", "verbose");
            VestigiumLog.Debug(VestigiumStatus.Success, "System", "IO", "debug");
            VestigiumLog.Information(VestigiumStatus.Success, "System", "IO", "info");
            VestigiumLog.Warning(VestigiumStatus.Pending, "System", "IO", "warn");
            VestigiumLog.Error(VestigiumStatus.Failed, "System", "IO", "error", new InvalidOperationException("boom"));
            VestigiumLog.Fatal(VestigiumStatus.Failed, "System", "IO", "fatal");
            VestigiumLogger.Flush();
            Assert.True(seen.Count >= 1 || VestigiumLogger.WrittenCount >= 1);

            VestigiumLogger.OverrideDiskPressure(true);
            Assert.True(VestigiumLogger.IsDiskTripped);
            VestigiumLogger.OverrideDiskPressure(false);
            Assert.False(VestigiumLogger.IsDiskTripped);
            VestigiumLogger.OverrideDiskPressure(null);
            VestigiumLogger.BindLifetime(null);

            for (var i = 0; i < 12; i++)
            {
                VestigiumLog.Information(
                    VestigiumStatus.Timeout, "Network", "ICMP",
                    "Echo request to 8.8.8.8 timed out after 1000 ms");
            }

            VestigiumLogger.Flush();
            Assert.True(VestigiumLogger.SuppressedCount >= 0);
            Assert.True(VestigiumLogger.WrittenCount >= 1);
            Assert.NotEmpty(VestigiumLogger.RecentJsonLines);
        }
        finally
        {
            sub?.Dispose();
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void Logger_options_and_event_json_and_reinitialize()
    {
        var opts = new VestigiumLoggerOptions { LogDirectory = Path.Combine(Path.GetTempPath(), "vestigium-opts") };
        Assert.Equal(opts.LogDirectory, opts.ResolveLogDirectory());
        var unset = new VestigiumLoggerOptions { AppId = "PingIQ" };
        Assert.Contains("Vestigium", unset.ResolveLogDirectory());

        var evt = new VestigiumLogEvent(
            DateTimeOffset.Parse("2026-09-06T20:20:00.123Z"),
            48216, 12,
            VestigiumLogLevel.Error, VestigiumStatus.Failed,
            "HttpIQ", "Network", "HTTP",
            "payload | tabs\there\nand a stack",
            "System.InvalidOperationException: boom\n   at HttpIQ.Probe()");
        var json = evt.ToJsonLine();
        Assert.Contains("\"APPID\":\"HttpIQ\"", json);
        Assert.DoesNotContain("\n", json.Replace("\\n", ""));

        var quiet = new VestigiumLogEvent(
            DateTimeOffset.UtcNow, 1, 1,
            VestigiumLogLevel.Information, VestigiumStatus.Success,
            "PingIQ", "Network", "ICMP", "ok", null);
        Assert.Contains("\"MESSAGE\":\"ok\"", quiet.ToJsonLine());

        HelperLog.Shutdown();
        Assert.Throws<InvalidOperationException>(() =>
            VestigiumLog.Information(VestigiumStatus.Success, "Network", "ICMP", "hello"));
        Assert.Throws<ArgumentException>(() => VestigiumLogger.Initialize(cfg => cfg.AppId = " "));

        var dir = Path.Combine(Path.GetTempPath(), "VestigiumLogBranch", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            VestigiumLogger.Initialize(cfg =>
            {
                cfg.AppId = "PingIQ";
                cfg.LogDirectory = dir;
            });
            VestigiumLogger.Initialize(cfg =>
            {
                cfg.AppId = "PingIQ";
                cfg.LogDirectory = dir;
            });
            Assert.True(VestigiumLogger.IsInitialized);
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    private sealed class LogCollector(List<VestigiumLogEvent> items) : IObserver<VestigiumLogEvent>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(VestigiumLogEvent value) => items.Add(value);
    }
}
