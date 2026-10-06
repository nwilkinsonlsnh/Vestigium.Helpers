using Vestigium.Helpers.LogParser;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class LogParserLp02Tests
{
    public LogParserLp02Tests()
    {
        VestigiumLogger.Shutdown();
    }

    [Fact]
    public void LP02_001_log_is_noop_when_uninitialized()
    {
        var thrown = Record.Exception(() =>
            LogParserLog.Error(LogParserEvents.HostRejected, VestigiumStatus.Failed, LogParserCatalog.Subcategories.Bag, "rejected host"));

        Assert.Null(thrown);
        Assert.False(VestigiumLogger.IsInitialized);
    }

    [Fact]
    public void LP02_002_directory_is_programdata_logparser()
    {
        Assert.Equal("LogParser", LogParserCatalog.AppId);
        Assert.EndsWith(Path.Combine("Vestigium", "Logs", "LogParser"), LogParserCatalog.LogDirectory);
    }
}
