using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ChartsLoggingTests
{
    public ChartsLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    [Fact]
    public void Xy_mismatch_writes_event_16530()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumChartsLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            VestigiumLogger.Initialize(cfg =>
            {
                cfg.AppId = ChartsCatalog.AppId;
                cfg.LogDirectory = dir;
                cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
                cfg.OperationsLogEnabled = false;
                ChartsCatalog.Register(cfg);
            });

            Assert.Throws<ArgumentException>(() =>
                ChartView.Scatter([1d, 2d], [1d]));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":16530"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Malformed_limits_write_event_16535()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumChartsLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            VestigiumLogger.Initialize(cfg =>
            {
                cfg.AppId = ChartsCatalog.AppId;
                cfg.LogDirectory = dir;
                cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
                cfg.OperationsLogEnabled = false;
                ChartsCatalog.Register(cfg);
                AnalyticsCatalog.Register(cfg);
            });

            var series = NumericSeries.From([1.0, 2.0, 3.0]);
            var bad = new ControlLimits { Center = 10, Upper = 5, Lower = 0 };
            Assert.Throws<ArgumentException>(() => ChartView.Control(series, bad));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":16535"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Reject_without_host_does_not_throw_logger()
    {
        var ex = Record.Exception(() =>
            ChartView.Scatter([1d], [1d, 2d]));
        Assert.IsType<ArgumentException>(ex);
    }
}
