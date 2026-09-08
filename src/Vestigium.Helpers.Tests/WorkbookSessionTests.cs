using System.IO.Compression;
using ClosedXML.Excel;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.ClosedXml;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class WorkbookSessionTests
{
    [Fact]
    public void Identity_is_stable()
        => Assert.Equal("Vestigium.Helpers.ClosedXml", WorkbookHelper.Identity);

    [Fact]
    public void WriteTable_round_trips_through_closedxml()
    {
        var path = TempXlsx();
        using (var book = WorkbookHelper.Create("Data", "ClosedXml"))
        {
            book.Sheet("Data").WriteTable(SheetTable.Create(
                ["Name", "Value"],
                [
                    ["alpha", 1],
                    ["beta", 2]
                ],
                "Data"));
            book.SaveAs(path);
        }

        Assert.True(new FileInfo(path).Length > 0);
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("Data");
        Assert.Equal("Name", ws.Cell(1, 1).GetString());
        Assert.Equal("Value", ws.Cell(1, 2).GetString());
        Assert.Equal("alpha", ws.Cell(2, 1).GetString());
        Assert.Equal(1d, ws.Cell(2, 2).GetDouble());
        Assert.Equal("beta", ws.Cell(3, 1).GetString());
        Assert.Equal(2d, ws.Cell(3, 2).GetDouble());
        Assert.True(ws.SheetView.SplitRow >= 1);
        Assert.True(ws.Tables.Any() || ws.AutoFilter.IsEnabled);
    }

    [Fact]
    public void Formula_like_text_is_not_evaluated()
    {
        var path = TempXlsx();
        using (var book = WorkbookHelper.Create("Inject", "ClosedXml"))
        {
            book.Sheet("Inject").WriteTable(SheetTable.Create(
                ["Payload"],
                [["=1+1"], ["+2"], ["-SUM(A1)"], ["@cmd"]]));
            book.SaveAs(path);
        }

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("Inject");
        for (var r = 2; r <= 5; r++)
        {
            var cell = ws.Cell(r, 1);
            Assert.False(cell.HasFormula);
            Assert.False(cell.DataType == XLDataType.Number && Math.Abs(cell.GetDouble() - 2d) < 0.0001);
        }

        Assert.Contains("1+1", ws.Cell(2, 1).GetString());
        Assert.StartsWith("'", ws.Cell(2, 1).GetString());
    }

    [Fact]
    public void NaN_is_rejected()
    {
        using var book = WorkbookHelper.Create("Bad", "ClosedXml");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            book.Sheet("Bad").WriteTable(SheetTable.Create(["X"], [[double.NaN]])));
    }

    [Fact]
    public void Infinity_is_rejected()
    {
        using var book = WorkbookHelper.Create("Bad", "ClosedXml");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            book.Sheet("Bad").WriteTable(SheetTable.Create(["X"], [[double.PositiveInfinity]])));
    }

    [Fact]
    public void TimeSpan_is_written_as_days()
    {
        var path = TempXlsx();
        using (var book = WorkbookHelper.Create("Span", "ClosedXml"))
        {
            book.Sheet("Span").WriteTable(SheetTable.Create(
                ["Duration"],
                [[TimeSpan.FromMinutes(90)]]));
            book.SaveAs(path);
        }

        using var wb = new XLWorkbook(path);
        var cell = wb.Worksheet("Span").Cell(2, 1);
        Assert.Equal(XLDataType.Number, cell.DataType);
        Assert.Equal(TimeSpan.FromMinutes(90).TotalDays, cell.GetDouble(), 12);
        Assert.Contains("h", cell.Style.NumberFormat.Format, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Open_missing_file_throws()
        => Assert.Throws<FileNotFoundException>(() =>
            WorkbookHelper.Open(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xlsx")));

    [Fact]
    public void Default_export_path_does_not_create_the_folder()
    {
        var dir = WorkbookHelper.DefaultExportDirectory("ClosedXml");
        Assert.Contains(Path.Combine("Vestigium", "Exports", "ClosedXml"), dir);
        var path = WorkbookHelper.NewExportPath("ClosedXml");
        Assert.StartsWith(dir, path);
        Assert.EndsWith(".xlsx", path);
        Assert.False(Directory.Exists(path));
    }

    [Fact]
    public void WriteSeries_creates_the_six_sheets()
    {
        var path = TempXlsx();
        var series = NumericSeries.From(Enumerable.Range(1, 9), "odd");
        using (var book = WorkbookHelper.Create("Summary", "ClosedXml"))
        {
            WorkbookHelper.WriteSeries(book, series, populationSize: 9);
            book.SaveAs(path);
        }

        using var wb = new XLWorkbook(path);
        Assert.True(wb.TryGetWorksheet("Summary", out var summary));
        Assert.True(wb.TryGetWorksheet("Bands", out _));
        Assert.True(wb.TryGetWorksheet("Confidence", out _));
        Assert.True(wb.TryGetWorksheet("Histogram", out _));
        Assert.True(wb.TryGetWorksheet("Sample", out var sample));
        Assert.Equal("n", summary.Cell(3, 1).GetString());
        Assert.Equal(9d, summary.Cell(3, 2).GetDouble());
        Assert.Equal(1d, sample.Cell(2, 2).GetDouble());
        Assert.Equal(9d, sample.Cell(10, 2).GetDouble());
        Assert.False(summary.TabColor.Equals(XLColor.NoColor));
        Assert.True(wb.TryGetWorksheet("Charts", out _));
        Assert.Equal("Charts", wb.Worksheet(2).Name);
    }

    [Fact]
    public void WriteSeries_embeds_excel_charts()
    {
        var path = TempXlsx();
        var series = NumericSeries.From(Enumerable.Range(1, 9), "odd");
        using (var book = WorkbookHelper.Create("Summary", "ClosedXml"))
        {
            WorkbookHelper.WriteSeries(book, series, populationSize: 9);
            Assert.True(book.Charts.Count >= 4);
            book.SaveAs(path);
        }

        using (var zip = ZipFile.OpenRead(path))
        {
            Assert.NotNull(zip.GetEntry("xl/charts/chart1.xml"));
            Assert.NotNull(zip.GetEntry("xl/drawings/drawing1.xml"));
            var names = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToArray();
            Assert.Contains(names, n => n.StartsWith("xl/charts/chart", StringComparison.Ordinal));
            using var stream = zip.GetEntry("xl/charts/chart1.xml")!.Open();
            using var reader = new StreamReader(stream);
            var xml = reader.ReadToEnd();
            Assert.Contains("c:barChart", xml);
        }

        using var wb = new XLWorkbook(path);
        Assert.Equal("n", wb.Worksheet("Summary").Cell(3, 1).GetString());
        Assert.Equal("90%", wb.Worksheet("Charts").Cell(2, 1).GetString());
    }

    [Fact]
    public void IncludeCharts_false_skips_chart_parts()
    {
        var path = TempXlsx();
        var series = NumericSeries.From(Enumerable.Range(1, 9), "odd");
        using (var book = WorkbookHelper.Create("Summary", "ClosedXml"))
        {
            book.IncludeCharts = false;
            WorkbookHelper.WriteSeries(book, series);
            Assert.Empty(book.Charts);
            book.SaveAs(path);
        }

        using var zip = ZipFile.OpenRead(path);
        Assert.Null(zip.GetEntry("xl/charts/chart1.xml"));
        using var wb = new XLWorkbook(path);
        Assert.False(wb.TryGetWorksheet("Charts", out _));
    }

    [Fact]
    public void OpenOrCreate_then_append_adds_rows()
    {
        var path = TempXlsx();
        using (var book = WorkbookHelper.Create("Log", "ClosedXml"))
        {
            book.Sheet("Log").WriteTable(SheetTable.Create(["Id"], [[1]]));
            book.SaveAs(path);
        }

        using (var book = WorkbookHelper.Open(path, "ClosedXml"))
        {
            book.Sheet("Log").AppendRows([[2], [3]]);
            book.Save();
        }

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("Log");
        Assert.Equal(1d, ws.Cell(2, 1).GetDouble());
        Assert.Equal(2d, ws.Cell(3, 1).GetDouble());
        Assert.Equal(3d, ws.Cell(4, 1).GetDouble());
    }

    [Fact]
    public void Illegal_sheet_names_are_sanitized()
    {
        using var book = WorkbookHelper.Create("Bad:Name*", "ClosedXml");
        Assert.Equal("Bad_Name_", book.SheetNames[0]);
        var extra = book.AddSheet("Bad:Name*");
        Assert.Equal("Bad_Name_ 2", extra.Name);
    }

    [Fact]
    public void Tab_color_is_applied()
    {
        var path = TempXlsx();
        using (var book = WorkbookHelper.Create("Tint", "ClosedXml"))
        {
            book.Sheet("Tint").WriteTable(
                SheetTable.Create(["X"], [[1]]),
                new SheetWriteOptions { TabColor = "#3EC6FF", CreateExcelTable = false });
            book.SaveAs(path);
        }

        using var wb = new XLWorkbook(path);
        Assert.Equal(XLColor.FromHtml("#3EC6FF"), wb.Worksheet("Tint").TabColor);
    }

    [Fact]
    public void Table_style_round_trips_an_excel_gallery_name()
    {
        var path = TempXlsx();
        using (var book = WorkbookHelper.Create("Data", "ClosedXml"))
        {
            book.TableStyle = "Medium9";
            book.Sheet("Data").WriteTable(SheetTable.Create(["Name", "Value"], [["alpha", 1]]));
            book.SaveAs(path);
        }

        using var wb = new XLWorkbook(path);
        var table = Assert.Single(wb.Worksheet("Data").Tables);
        Assert.Equal("TableStyleMedium9", table.Theme.Name);
    }

    [Fact]
    public void Unknown_table_style_is_rejected()
    {
        using var book = WorkbookHelper.Create("Data", "ClosedXml");
        Assert.Throws<ArgumentOutOfRangeException>(() => book.TableStyle = "ComicSans3");
    }

    [Fact]
    public void Default_table_style_is_excel_medium_2()
    {
        using var book = WorkbookHelper.Create("Data", "ClosedXml");
        Assert.Equal("Medium2", book.TableStyle);
        Assert.Contains("Medium2", WorkbookHelper.TableStyles);
        Assert.Equal(60, WorkbookHelper.TableStyles.Count);
    }

    private static string TempXlsx()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "book.xlsx");
    }
}
