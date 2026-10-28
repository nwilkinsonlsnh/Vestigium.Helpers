using Vestigium.Helpers.Processes;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessesLoggingTests
{
    public ProcessesLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    private static void Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumProcLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = ProcessesCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            ProcessesCatalog.Register(cfg);
        });
    }

    [Fact]
    public void Probe_writes_15005()
    {
        try
        {
            Init();
            Assert.Equal("Vestigium.Helpers.Processes", ProcessHelper.Probe());
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":15005"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Probe_without_host_does_not_throw()
    {
        Assert.Equal("Vestigium.Helpers.Processes", ProcessHelper.Probe());
    }
}
