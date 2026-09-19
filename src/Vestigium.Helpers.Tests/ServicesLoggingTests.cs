using Vestigium.Helpers.Services;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ServicesLoggingTests
{
    public ServicesLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    private static void Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumSvcLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = ServicesCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            ServicesCatalog.Register(cfg);
        });
    }

    [Fact]
    public void Probe_writes_15505()
    {
        try
        {
            Init();
            Assert.Equal("Vestigium.Helpers.Services", ServiceHelper.Probe());
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":15505"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Probe_without_host_does_not_throw()
    {
        Assert.Equal("Vestigium.Helpers.Services", ServiceHelper.Probe());
    }
}
