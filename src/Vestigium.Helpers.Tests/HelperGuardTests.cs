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

    [Fact]
    public void NotEmpty_InRange_Finite_and_state_guards()
    {
        Assert.Throws<ArgumentNullException>(() => HelperGuard.NotEmpty<int>(null, "items"));
        var empty = Assert.Throws<ArgumentException>(() => HelperGuard.NotEmpty(Array.Empty<int>(), "items"));
        Assert.Equal("items", empty.ParamName);
        Assert.Contains("Value must contain at least one item.", empty.Message);
        Assert.Equal([1], HelperGuard.NotEmpty(new[] { 1 }, "items"));

        var range = Assert.Throws<ArgumentOutOfRangeException>(() => HelperGuard.InRange(0, 1, "row"));
        Assert.Equal("row", range.ParamName);
        Assert.Contains("row must be at least 1", range.Message, StringComparison.Ordinal);
        Assert.Equal(2, HelperGuard.InRange(2, 1, "row"));

        Assert.Throws<ArgumentOutOfRangeException>(() => HelperGuard.Finite(double.NaN, "v"));
        Assert.Throws<ArgumentOutOfRangeException>(() => HelperGuard.Finite(float.PositiveInfinity, "v"));
        Assert.Equal(1.5d, HelperGuard.Finite(1.5d, "v"));
        Assert.Equal(1.5f, HelperGuard.Finite(1.5f, "v"));

        HelperGuard.Require(true, "chart", "ok");
        var require = Assert.Throws<ArgumentException>(() => HelperGuard.Require(false, "chart", "A chart needs a sheet name."));
        Assert.Equal("chart", require.ParamName);
        Assert.Contains("A chart needs a sheet name.", require.Message);

        HelperGuard.RequireState(true, "ok");
        var state = Assert.Throws<InvalidOperationException>(() =>
            HelperGuard.RequireState(false, "A workbook must keep at least one worksheet."));
        Assert.Equal("A workbook must keep at least one worksheet.", state.Message);

        HelperGuard.NotDisposed(false, this);
        Assert.Throws<ObjectDisposedException>(() => HelperGuard.NotDisposed(true, this));
    }

    [Fact]
    public void FileExists_rejects_blank_and_missing()
    {
        var blank = Assert.Throws<ArgumentException>(() => HelperGuard.FileExists("  ", "path"));
        Assert.Equal("path", blank.ParamName);
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "nope.txt");
        var ex = Assert.Throws<FileNotFoundException>(() => HelperGuard.FileExists(missing, "path"));
        Assert.Equal(missing, ex.FileName);
        Assert.Contains("path was not found", ex.Message, StringComparison.Ordinal);

        var dir = NewDir();
        var file = Path.Combine(dir, "held.txt");
        File.WriteAllText(file, "x");
        Assert.Equal(file, HelperGuard.FileExists(file, "path"));
    }

    [Fact]
    public void Core_probe_logs_pending_then_success()
    {
        var dir = NewDir();
        HelperLog.InitializeHost(HelperLog.AppIds.Core, cfg => cfg.LogDirectory = dir);
        try
        {
            Assert.Equal("Vestigium.Helpers", HelperGuard.Probe());
            Assert.Contains(HelperLog.RecentJsonLines, l => l.Contains("\"STATUS\":\"Pending\""));
            Assert.Contains(HelperLog.RecentJsonLines, l =>
                l.Contains("\"STATUS\":\"Success\"") && l.Contains("Identity=Vestigium.Helpers"));
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
