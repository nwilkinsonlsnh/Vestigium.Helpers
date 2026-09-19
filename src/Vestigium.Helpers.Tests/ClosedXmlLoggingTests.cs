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

    private static string Init(string app = ClosedXmlCatalog.AppId)
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumClosedXmlLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = app;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            ClosedXmlCatalog.Register(cfg);
        });
        return dir;
    }

    [Fact]
    public void Create_writes_event_11015()
    {
        try
        {
            Init();
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
        try
        {
            var dir = Init();
            Assert.Throws<FileNotFoundException>(() => WorkbookHelper.Open(Path.Combine(dir, "no-such.xlsx")));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11035"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void SaveAs_writes_event_11025_not_sheet_wrote()
    {
        try
        {
            var dir = Init();
            using var book = WorkbookHelper.Create("SaveTest");
            book.Sheet("SaveTest").WriteTable(SheetTable.Create(["A", "B"], [[1, 2]]));
            book.SaveAs(Path.Combine(dir, "out.xlsx"));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11025"));
            Assert.DoesNotContain(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11050"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Formula_like_text_writes_event_11070()
    {
        try
        {
            Init();
            using var book = WorkbookHelper.Create("Safe");
            book.Sheet("Safe").WriteTable(SheetTable.Create(["A"], [["=1+1"]]));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11070"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Save_with_chart_writes_event_11085()
    {
        try
        {
            var dir = Init();
            using var book = WorkbookHelper.Create("Charts");
            book.Sheet("Charts").WriteTable(SheetTable.Create(["X", "Y"], [[1d, 2d], [3d, 4d]]));
            book.AddChart(new SheetChart
            {
                Sheet = "Charts",
                Title = "Y",
                CategoriesFormula = "Charts!$A$2:$A$3",
                Categories = ["1", "3"],
                Series =
                [
                    new ChartSeries
                    {
                        Name = "Y",
                        ValuesFormula = "Charts!$B$2:$B$3",
                        Values = [2d, 4d]
                    }
                ]
            });
            book.SaveAs(Path.Combine(dir, "charts.xlsx"));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11060"));
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11085"));
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":11025"));
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
