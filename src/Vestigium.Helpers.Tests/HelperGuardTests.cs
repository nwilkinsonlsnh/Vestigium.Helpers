using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.ClosedXml;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class HelperGuardTests
{
    public HelperGuardTests()
    {
        HelperLog.Shutdown();
    }

    [Fact]
    public void Reject_before_initialize_still_throws()
    {
        Assert.False(HelperLog.IsInitialized);
        var ex = Assert.Throws<ArgumentNullException>(() => HelperGuard.NotNull<string>(null, "table"));
        Assert.Equal("table", ex.ParamName);
        Assert.Empty(HelperLog.RecentJsonLines);
    }

    [Fact]
    public void NotNull_writes_error_then_throws()
    {
        var dir = NewDir();
        HelperLog.InitializeHost(HelperLog.AppIds.Core, cfg => cfg.LogDirectory = dir);
        try
        {
            Assert.Throws<ArgumentNullException>(() => HelperGuard.NotNull<string>(null, "table"));
            Assert.Contains(HelperLog.RecentJsonLines, l =>
                l.Contains("\"LEVEL\":\"Error\"")
                && l.Contains("\"STATUS\":\"Failed\"")
                && l.Contains("reject")
                && l.Contains("table is null"));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void Enter_is_debug_in_the_ring()
    {
        var dir = NewDir();
        HelperLog.InitializeHost(HelperLog.AppIds.ClosedXml, cfg =>
        {
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Information;
        });
        try
        {
            using (HelperLog.Begin(HelperLog.AppIds.ClosedXml, HelperLog.Subcategories.Session, "Create", "firstSheet=Probe", "abc123"))
            {
                HelperLog.Information(
                    HelperLog.AppIds.ClosedXml,
                    VestigiumStatus.Success,
                    HelperLog.Subcategories.Session,
                    "created session=abc123 sheets=1 path=(new)");
            }

            Assert.Contains(HelperLog.RecentJsonLines, l =>
                l.Contains("\"LEVEL\":\"Debug\"")
                && l.Contains("enter Create")
                && l.Contains("firstSheet=Probe")
                && l.Contains("abc123"));
            Assert.Contains(HelperLog.RecentJsonLines, l =>
                l.Contains("\"LEVEL\":\"Information\"")
                && l.Contains("created session=abc123"));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void Debug_enter_is_not_written_to_disk_when_floor_is_information()
    {
        var dir = NewDir();
        HelperLog.InitializeHost(HelperLog.AppIds.Core, cfg =>
        {
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Information;
        });
        try
        {
            HelperLog.Enter(HelperLog.AppIds.Core, HelperLog.Subcategories.Session, "Create", "firstSheet=Probe", "abc123");
            HelperLog.Information(
                HelperLog.AppIds.Core,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Session,
                "created session=abc123");
            HelperLog.Flush();

            var disk = Directory.GetFiles(dir, "vestigium-*.json");
            Assert.NotEmpty(disk);
            var text = string.Join(Environment.NewLine, disk.Select(File.ReadAllText));
            Assert.DoesNotContain("enter Create", text, StringComparison.Ordinal);
            Assert.Contains("created session=abc123", text, StringComparison.Ordinal);
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void Debug_enter_is_written_to_disk_when_floor_is_debug()
    {
        var dir = NewDir();
        HelperLog.InitializeHost(HelperLog.AppIds.Core, cfg =>
        {
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
        });
        try
        {
            HelperLog.Enter(HelperLog.AppIds.Core, HelperLog.Subcategories.Session, "Create", "firstSheet=Probe", "abc123");
            HelperLog.Flush();

            var disk = Directory.GetFiles(dir, "vestigium-*.json");
            Assert.NotEmpty(disk);
            var text = string.Join(Environment.NewLine, disk.Select(File.ReadAllText));
            Assert.Contains("enter Create", text, StringComparison.Ordinal);
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void Taxonomy_registers_story_subcategories()
    {
        foreach (var sub in new[]
        {
            HelperLog.Subcategories.Guard,
            HelperLog.Subcategories.Session,
            HelperLog.Subcategories.Sheet,
            HelperLog.Subcategories.Series,
            HelperLog.Subcategories.Confidence,
            HelperLog.Subcategories.Chart
        })
        {
            Assert.True(HelperLog.Taxonomy.IsSubcategoryRegistered(HelperLog.Category, sub), sub);
        }
    }

    [Fact]
    public void Workbook_session_id_is_on_the_save_line()
    {
        var dir = NewDir();
        HelperLog.InitializeHost(HelperLog.AppIds.ClosedXml, cfg => cfg.LogDirectory = dir);
        try
        {
            var path = Path.Combine(dir, "story.xlsx");
            using var book = WorkbookHelper.Create("Data", HelperLog.AppIds.ClosedXml);
            book.Sheet("Data").WriteTable(SheetTable.Create(["A"], [["x"]]), new SheetWriteOptions { CreateExcelTable = false, Autosize = false });
            book.SaveAs(path);
            Assert.False(string.IsNullOrWhiteSpace(book.SessionId));
            Assert.Contains(HelperLog.RecentJsonLines, l =>
                l.Contains("enter Create") && l.Contains($"session={book.SessionId}"));
            Assert.Contains(HelperLog.RecentJsonLines, l =>
                l.Contains("Saved workbook") && l.Contains($"session={book.SessionId}"));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void Empty_series_logs_reject_then_throws()
    {
        var dir = NewDir();
        HelperLog.InitializeHost(HelperLog.AppIds.Analytics, cfg => cfg.LogDirectory = dir);
        try
        {
            Assert.Throws<ArgumentException>(() => NumericSeries.From(Array.Empty<double>()));
            Assert.Contains(HelperLog.RecentJsonLines, l =>
                l.Contains("\"LEVEL\":\"Error\"")
                && l.Contains("reject")
                && l.Contains("empty"));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void Series_construct_logs_n_and_series_id()
    {
        var dir = NewDir();
        HelperLog.InitializeHost(HelperLog.AppIds.Analytics, cfg => cfg.LogDirectory = dir);
        try
        {
            var series = NumericSeries.From(new[] { 1, 2, 3 }, "demo");
            Assert.False(string.IsNullOrWhiteSpace(series.SeriesId));
            Assert.Contains(HelperLog.RecentJsonLines, l =>
                l.Contains("constructed")
                && l.Contains($"series={series.SeriesId}")
                && l.Contains("n=3")
                && l.Contains("name=demo"));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void Open_garbage_file_logs_failed_through_vestigium_logging()
    {
        var dir = NewDir();
        HelperLog.InitializeHost(HelperLog.AppIds.ClosedXml, cfg => cfg.LogDirectory = dir);
        try
        {
            var junk = Path.Combine(dir, "not-excel.xlsx");
            File.WriteAllText(junk, "this is not a workbook");
            Assert.ThrowsAny<Exception>(() => WorkbookHelper.Open(junk, HelperLog.AppIds.ClosedXml));
            Assert.Contains(HelperLog.RecentJsonLines, l =>
                l.Contains("\"APPID\":\"ClosedXml\"")
                && l.Contains("\"LEVEL\":\"Error\"")
                && l.Contains("\"STATUS\":\"Failed\"")
                && l.Contains("\"CATEGORY\":\"Helpers\""));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    private static string NewDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
