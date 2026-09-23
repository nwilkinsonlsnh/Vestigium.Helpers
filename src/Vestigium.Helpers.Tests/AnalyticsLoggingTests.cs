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
                cfg.FloodThresholdCount = 100;
                AnalyticsCatalog.Register(cfg);
            });

            Assert.Throws<ArgumentException>(() => NumericSeries.From(Array.Empty<double>()));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("10520", StringComparison.Ordinal));
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
                cfg.FloodThresholdCount = 100;
                AnalyticsCatalog.Register(cfg);
            });

            var series = NumericSeries.From([10.0, 10.1, 9.9, 10.2, 9.8, 100.0], "spike");
            var limits = series.ControlLimits(k: 1);
            Assert.True(limits.OutOfControlCount > 0);
            VestigiumLogger.Flush();
            var lines = VestigiumLogger.RecentJsonLines;
            Assert.True(
                lines.Any(line => line.Contains("10595", StringComparison.Ordinal)
                    || line.Contains("LimitsOutOfControl", StringComparison.Ordinal)
                    || line.Contains("out-of-control points", StringComparison.Ordinal)),
                "Missing 10595. Ring:\n" + string.Join(Environment.NewLine, lines));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Construct_without_host_does_not_throw()
    {
        var series = NumericSeries.From([1.0, 2.0, 3.0]);
        Assert.Equal(3, series.Count);
    }
}
