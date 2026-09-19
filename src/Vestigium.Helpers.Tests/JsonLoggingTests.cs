using Vestigium.Helpers.Json;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class JsonLoggingTests
{
    public JsonLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    private static string Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumJsonLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = JsonCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            JsonCatalog.Register(cfg);
        });
        return dir;
    }

    [Fact]
    public void Probe_writes_13505()
    {
        try
        {
            Init();
            Assert.Equal("Vestigium.Helpers.Json", JsonHelper.Probe());
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":13505"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Probe_without_host_does_not_throw()
    {
        Assert.Equal("Vestigium.Helpers.Json", JsonHelper.Probe());
    }
}
