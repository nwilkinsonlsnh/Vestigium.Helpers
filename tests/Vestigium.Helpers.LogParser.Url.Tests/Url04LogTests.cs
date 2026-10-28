using Vestigium.Logging;

namespace Vestigium.Helpers.LogParser.Url.Tests;

public sealed class Url04LogTests
{
    [Fact]
    public void URL04_001_log_is_noop_when_uninitialized()
    {
        VestigiumLogger.Shutdown();
        var thrown = Record.Exception(() => UrlLog.Error(UrlEvents.ScanFailed, "scan failed"));
        Assert.Null(thrown);
        Assert.False(VestigiumLogger.IsInitialized);
    }

    [Fact]
    public void URL04_002_directory_is_programdata_logparser_url()
    {
        Assert.Equal("LogParser.Url", UrlCatalog.AppId);
        Assert.EndsWith(Path.Combine("Vestigium", "Logs", "LogParser.Url"), UrlCatalog.LogDirectory);
    }
}
