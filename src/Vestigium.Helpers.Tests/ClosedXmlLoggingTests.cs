using Vestigium.Helpers.ClosedXml;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ClosedXmlLoggingTests
{
    public ClosedXmlLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    [Fact]
    public void Create_writes_event_11015()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumClosedXmlLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            VestigiumLogger.Initialize(cfg =>
            {
                cfg.AppId = ClosedXmlCatalog.AppId;
                cfg.LogDirectory = dir;
                cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
                cfg.OperationsLogEnabled = false;
                ClosedXmlCatalog.Register(cfg);
            });

            using var book = WorkbookHelper.Create("LogTest");
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11015"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Open_missing_file_writes_event_11035()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumClosedXmlLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            VestigiumLogger.Initialize(cfg =>
            {
                cfg.AppId = ClosedXmlCatalog.AppId;
                cfg.LogDirectory = dir;
                cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
                cfg.OperationsLogEnabled = false;
                ClosedXmlCatalog.Register(cfg);
            });

            var missing = Path.Combine(dir, "no-such.xlsx");
            Assert.Throws<FileNotFoundException>(() => WorkbookHelper.Open(missing));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11035"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Create_without_host_does_not_throw()
    {
        using var book = WorkbookHelper.Create();
        Assert.NotEmpty(book.SessionId);
    }
}
