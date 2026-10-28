using Vestigium.Helpers.Csv;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class CsvLoggingTests
{
    public CsvLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    private static string Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumCsvLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = CsvCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            CsvCatalog.Register(cfg);
        });
        return dir;
    }

    [Fact]
    public void Create_and_SaveAs_write_11515_and_11525()
    {
        try
        {
            var dir = Init();
            using var file = CsvHelper.Create();
            file.WriteTable(CsvTable.Create(["A"], [["x"]]));
            file.SaveAs(Path.Combine(dir, "out.csv"));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11515"));
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11525"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Open_missing_file_writes_11530()
    {
        try
        {
            var dir = Init();
            Assert.Throws<FileNotFoundException>(() => CsvHelper.Open(Path.Combine(dir, "no.csv")));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11530"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Unclosed_quote_writes_11540()
    {
        try
        {
            Init();
            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("A\r\n\"open"));
            Assert.Throws<CsvFormatException>(() => CsvHelper.Read(ms));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11540"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Formula_like_text_writes_11550()
    {
        try
        {
            Init();
            using var file = CsvHelper.Create();
            file.WriteTable(CsvTable.Create(["A"], [["=1+1"]]));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11550"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Create_without_host_does_not_throw()
    {
        using var file = CsvHelper.Create();
        Assert.NotEmpty(file.SessionId);
    }
}
