using Vestigium.Helpers.Xml;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class XmlLoggingTests
{
    public XmlLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    private static void Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumXmlLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = XmlCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            XmlCatalog.Register(cfg);
        });
    }

    [Fact]
    public void Probe_writes_16505()
    {
        try
        {
            Init();
            Assert.Equal("Vestigium.Helpers.Xml", XmlHelper.Probe());
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":16505"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Probe_without_host_does_not_throw()
    {
        Assert.Equal("Vestigium.Helpers.Xml", XmlHelper.Probe());
    }
}
