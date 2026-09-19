using Vestigium.Helpers.FileIo;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class FileIoLoggingTests
{
    public FileIoLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    private static string Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumFileIoLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = FileIoCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            FileIoCatalog.Register(cfg);
        });
        return dir;
    }

    [Fact]
    public void Probe_writes_12505()
    {
        try
        {
            Init();
            Assert.Equal("Vestigium.Helpers.FileIo", FileIoHelper.Probe());
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":12505"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Analyze_missing_directory_writes_12520()
    {
        try
        {
            var dir = Init();
            var missing = Path.Combine(dir, "no-such-dir");
            Assert.Throws<DirectoryNotFoundException>(() => FileIoHelper.AnalyzeDirectory(missing));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":12520"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Probe_without_host_does_not_throw()
    {
        Assert.Equal("Vestigium.Helpers.FileIo", FileIoHelper.Probe());
    }
}
