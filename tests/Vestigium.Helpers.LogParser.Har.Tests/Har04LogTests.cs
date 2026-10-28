using Vestigium.Logging;

namespace Vestigium.Helpers.LogParser.Har.Tests;

public sealed class Har04LogTests
{
    [Fact]
    public void HAR04_001_log_is_noop_when_uninitialized()
    {
        VestigiumLogger.Shutdown();
        var thrown = Record.Exception(() => HarLog.Error(HarEvents.ParseFailed, "parse failed"));
        Assert.Null(thrown);
        Assert.False(VestigiumLogger.IsInitialized);
    }

    [Fact]
    public void HAR04_002_directory_is_programdata_logparser_har()
    {
        Assert.Equal("LogParser.Har", HarCatalog.AppId);
        Assert.EndsWith(Path.Combine("Vestigium", "Logs", "LogParser.Har"), HarCatalog.LogDirectory);
    }
}
