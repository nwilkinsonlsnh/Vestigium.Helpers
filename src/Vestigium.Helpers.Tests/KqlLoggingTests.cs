using Vestigium.Helpers.Kql;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class KqlLoggingTests
{
    public KqlLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    private static void Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumKqlLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = KqlLoggingCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            KqlLoggingCatalog.Register(cfg);
        });
    }

    [Fact]
    public void Probe_writes_14005()
    {
        try
        {
            Init();
            Assert.Equal("Vestigium.Helpers.Kql", KqlHelper.Probe());
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":14005"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Probe_without_host_does_not_throw()
    {
        Assert.Equal("Vestigium.Helpers.Kql", KqlHelper.Probe());
    }
}
