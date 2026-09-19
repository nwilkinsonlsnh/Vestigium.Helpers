using Vestigium.Helpers.Hashing;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class HashingLoggingTests
{
    public HashingLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    private static string Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = HashingCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            HashingCatalog.Register(cfg);
        });
        return dir;
    }

    [Fact]
    public void Probe_writes_13005()
    {
        try
        {
            Init();
            Assert.Equal("Vestigium.Helpers.Hashing", HashingHelper.Probe());
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":13005"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Probe_without_host_does_not_throw()
    {
        Assert.Equal("Vestigium.Helpers.Hashing", HashingHelper.Probe());
    }
}
