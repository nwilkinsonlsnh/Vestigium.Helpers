using Vestigium.Helpers.WinReg;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class WinRegLoggingTests
{
    public WinRegLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    private static void Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumWinRegLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = WinRegCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            WinRegCatalog.Register(cfg);
        });
    }

    [Fact]
    public void Probe_writes_16005()
    {
        try
        {
            Init();
            Assert.Equal("Vestigium.Helpers.WinReg", RegistryHelper.Probe());
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":16005"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Probe_without_host_does_not_throw()
    {
        Assert.Equal("Vestigium.Helpers.WinReg", RegistryHelper.Probe());
    }
}
