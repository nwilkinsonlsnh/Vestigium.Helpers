using Vestigium.Helpers.Analytics;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class AnalyticsLoggingTests
{
    public AnalyticsLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    [Fact]
    public void Empty_series_writes_event_10520()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumAnalyticsLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            VestigiumLogger.Initialize(cfg =>
            {
                cfg.AppId = AnalyticsCatalog.AppId;
                cfg.LogDirectory = dir;
                cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
                cfg.OperationsLogEnabled = false;
                AnalyticsCatalog.Register(cfg);
            });

            Assert.Throws<ArgumentException>(() => NumericSeries.From(Array.Empty<double>()));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":10520"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Out_of_control_writes_event_10595()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumAnalyticsLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            VestigiumLogger.Initialize(cfg =>
            {
                cfg.AppId = AnalyticsCatalog.AppId;
                cfg.LogDirectory = dir;
                cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
                cfg.OperationsLogEnabled = false;
                AnalyticsCatalog.Register(cfg);
            });

            var series = NumericSeries.From(new[] { 10.0, 10.1, 9.9, 10.2, 9.8, 100.0 }, "spike");
            var limits = series.ControlLimits();
            Assert.True(limits.OutOfControlCount > 0);
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":10595"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Construct_without_host_does_not_throw()
    {
        var series = NumericSeries.From(new[] { 1.0, 2.0, 3.0 });
        Assert.Equal(3, series.Count);
    }
}
