using ClosedXML.Excel;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.ClosedXml;
using CxChartKind = Vestigium.Helpers.ClosedXml.ChartKind;
using CxChartSeries = Vestigium.Helpers.ClosedXml.ChartSeries;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ClosedXmlBranchTests
{
    [Fact]
    public void ExcelNames_sanitize_empty_illegal_long_and_digit_table()
    {
        Assert.Equal("Sheet1", ExcelNames.Sanitize(null));
        Assert.Equal("Sheet1", ExcelNames.Sanitize("   "));
        Assert.Equal("Sheet1", ExcelNames.Sanitize("'''"));
        Assert.Equal("a_b_c", ExcelNames.Sanitize("a/b*c"));
        var longName = new string('N', 40);
        Assert.Equal(31, ExcelNames.Sanitize(longName).Length);

        Assert.Equal("T_123abc", ExcelNames.SanitizeTable("123abc", "Data"));
        Assert.Equal("T_", ExcelNames.SanitizeTable("***", "Data")[..2]);
        Assert.Equal("Data", ExcelNames.SanitizeTable("   ", "Data"));
        var huge = ExcelNames.SanitizeTable(new string('A', 250), "Data");
        Assert.True(huge.Length <= 200);

        Assert.Equal("Data", ExcelNames.SanitizeDefinedName("123bad"));
        Assert.Equal("A_B.C", ExcelNames.SanitizeDefinedName("A B.C"));
        Assert.Equal(255, ExcelNames.SanitizeDefinedName(new string('Z', 300)).Length);
        Assert.Equal("___", ExcelNames.Sanitize("???"));
        Assert.Equal("'O''Brien'", ExcelNames.QuoteSheet("O'Brien"));
    }

    [Fact]
    public void ExcelNames_tab_color_column_letter_and_quote_sheet()
    {
        Assert.Null(ExcelNames.ParseTabColor(null));
        Assert.Null(ExcelNames.ParseTabColor(" "));
        Assert.Null(ExcelNames.ParseTabColor("zz00zz"));
        Assert.Null(ExcelNames.ParseTabColor("12345"));
        Assert.NotNull(ExcelNames.ParseTabColor("#3EC6FF"));
        Assert.NotNull(ExcelNames.ParseTabColor("FF3EC6FF"));
        Assert.NotNull(ExcelNames.ParseTabColor("3ec6ff"));

        Assert.Equal("A", ExcelNames.ColumnLetter(1));
        Assert.Equal("Z", ExcelNames.ColumnLetter(26));
        Assert.Equal("AA", ExcelNames.ColumnLetter(27));
        Assert.Equal("AB", ExcelNames.ColumnLetter(28));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExcelNames.ColumnLetter(0));

        Assert.Equal("Sheet1", ExcelNames.QuoteSheet("Sheet1"));
        Assert.Equal("'1Sheet'", ExcelNames.QuoteSheet("1Sheet"));
        Assert.Contains("'", ExcelNames.QuoteSheet("Sheet 1"));
        var a1 = ExcelNames.A1Range("Data", 1, 1, 2, 9);
        Assert.Contains("$A$1:$B$9", a1);
        Assert.Equal("AAA", ExcelNames.ColumnLetter(703));
    }

    [Fact]
    public void Table_styles_normalize_none_excel_name_and_unknown()
    {
        Assert.Equal(ExcelTableStyles.DefaultId, ExcelTableStyles.Normalize(null));
        Assert.Equal(ExcelTableStyles.DefaultId, ExcelTableStyles.Normalize("  "));
        Assert.Equal("None", ExcelTableStyles.Normalize("None"));
        Assert.Equal("None", ExcelTableStyles.Normalize("none"));
        Assert.Equal("Medium2", ExcelTableStyles.Normalize("TableStyleMedium2"));
        Assert.Equal("Medium2", ExcelTableStyles.Normalize(" TableStyleMedium2 "));
        Assert.Equal("Light1", ExcelTableStyles.Normalize("LIGHT1"));
        Assert.Equal("Dark11", ExcelTableStyles.Normalize("dark11"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExcelTableStyles.Normalize("Medium99"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExcelTableStyles.Normalize("123"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExcelTableStyles.Normalize("Medium"));

        Assert.Equal("None", ExcelTableStyles.ToExcelName("None"));
        Assert.Equal("TableStyleMedium2", ExcelTableStyles.ToExcelName("Medium2"));
        Assert.False(ExcelTableStyles.IsKnown(null));
        Assert.False(ExcelTableStyles.IsKnown(" "));
        Assert.True(ExcelTableStyles.IsKnown("None"));
        Assert.True(ExcelTableStyles.IsKnown("Medium2"));
        Assert.NotNull(ExcelTableStyles.Resolve("Medium2"));
        Assert.NotEmpty(ExcelTableStyles.Ids);
        Assert.Contains("Light21", ExcelTableStyles.Ids);
        Assert.Equal(ExcelTableStyles.Ids, WorkbookHelper.TableStyles);
    }

    [Fact]
    public void OpenOrCreate_mints_then_reopens_without_mixing_saveto()
    {
        var path = TempXlsx();
        using (var created = WorkbookHelper.OpenOrCreate(path, "First", "ClosedXml"))
        {
            created.Sheet("First").WriteTable(SheetTable.Create(["N"], [[1]]));
            created.AddChart(MakeChart("First", "N", CxChartKind.Column));
            created.SaveAs(path);
        }

        using (var reopened = WorkbookHelper.OpenOrCreate(path, "First", "ClosedXml"))
        {
            Assert.Contains("First", reopened.SheetNames);
            using var noCharts = new MemoryStream();
            reopened.IncludeCharts = false;
            reopened.SaveTo(noCharts);
            Assert.True(noCharts.Length > 0);
        }

        var minted = Path.Combine(Path.GetTempPath(), "VestigiumClosedXmlTests", Guid.NewGuid().ToString("N"), "new.xlsx");
        Directory.CreateDirectory(Path.GetDirectoryName(minted)!);
        using var createdMissing = WorkbookHelper.OpenOrCreate(minted, "Sheet1", "ClosedXml");
        createdMissing.Dispose();
        Assert.True(File.Exists(minted));

        using var sameName = WorkbookHelper.Create("Sheet1", "ClosedXml");
        Assert.Contains("Sheet1", sameName.SheetNames);
        using var blank = WorkbookHelper.Create(null, "ClosedXml");
        Assert.NotEmpty(blank.SheetNames);
    }

    [Fact]
    public void SaveTo_embeds_every_chart_kind_and_two_series()
    {
        using var book = WorkbookHelper.Create("Data", "ClosedXml");
        book.Sheet("Data").WriteTable(SheetTable.Create(["A", "B"], [["x", 1], ["y", 2]]));
        foreach (var kind in new[] { CxChartKind.Column, CxChartKind.Bar, CxChartKind.Line, CxChartKind.Pie, CxChartKind.Scatter })
            book.AddChart(MakeChart("Data", kind.ToString(), kind, twoSeries: true));

        using var buffer = new MemoryStream();
        book.SaveTo(buffer);
        Assert.True(buffer.Length > 1000);
        Assert.Equal(5, book.Charts.Count);
    }

    [Fact]
    public void AddChart_rejects_blank_sheet_and_empty_series()
    {
        using var book = WorkbookHelper.Create("Data", "ClosedXml");
        Assert.Throws<ArgumentException>(() => book.AddChart(new SheetChart
        {
            Sheet = "  ",
            Title = "x",
            CategoriesFormula = "A1",
            Series =
            [
                new CxChartSeries { Name = "s", ValuesFormula = "B1", Values = [1] }
            ]
        }));
        Assert.Throws<ArgumentException>(() => book.AddChart(new SheetChart
        {
            Sheet = "Data",
            Title = "x",
            CategoriesFormula = "A1",
            Series = []
        }));
    }

    [Fact]
    public void WriteSeries_prefix_and_long_sheet_name()
    {
        var path = TempXlsx();
        var series = NumericSeries.From(Enumerable.Range(1, 5), "seq");
        using (var book = WorkbookHelper.Create(new string('S', 40), "ClosedXml"))
        {
            book.TableStyle = "Light9";
            WorkbookHelper.WriteSeries(book, series, prefix: "Run", populationSize: 5, tableStyle: "Light1");
            book.SaveAs(path);
        }

        using var wb = new XLWorkbook(path);
        Assert.Contains(wb.Worksheets, w => w.Name.Contains("Run", StringComparison.Ordinal) || w.Name.Contains("Summary", StringComparison.Ordinal));
    }

    [Fact]
    public void WorkbookHelper_export_paths_open_template_and_remove_sheet()
    {
        Assert.Contains(Path.Combine("Vestigium", "Exports", "ClosedXml"), WorkbookHelper.DefaultExportDirectory("ClosedXml"));
        Assert.Throws<ArgumentException>(() => WorkbookHelper.DefaultExportDirectory(" "));
        var named = WorkbookHelper.NewExportPath("ClosedXml", "nathan");
        Assert.Contains("nathan-", Path.GetFileName(named));
        Assert.EndsWith(".xlsx", named);
        var stamped = WorkbookHelper.NewExportPath("ClosedXml");
        Assert.Contains("vestigium-ClosedXml-", Path.GetFileName(stamped));

        var path = TempXlsx();
        using (var book = WorkbookHelper.Create("Letter", "ClosedXml"))
        {
            book.Sheet("Letter").WriteTable(SheetTable.Create(["N"], [[1]]));
            book.AddSheet("Keep");
            Assert.True(book.RemoveSheet("Keep"));
            Assert.False(book.RemoveSheet("Missing"));
            Assert.Throws<InvalidOperationException>(() => book.RemoveSheet("Letter"));
            book.SaveAs(path);
        }

        using var opened = WorkbookHelper.OpenTemplate(path, "ClosedXml");
        Assert.Contains("Letter", opened.SheetNames);
        var saved = opened.Save();
        Assert.Equal(path, saved);
    }

    static SheetChart MakeChart(string sheet, string title, CxChartKind kind, bool twoSeries = false)
    {
        var series = new List<CxChartSeries>
        {
            new() { Name = "S1", ValuesFormula = $"{sheet}!$B$2:$B$3", Values = [1, 2], Color = "1F4E79" }
        };
        if (twoSeries)
            series.Add(new CxChartSeries { Name = "S2", ValuesFormula = $"{sheet}!$B$2:$B$3", Values = [2, 1] });
        return new SheetChart
        {
            Sheet = sheet,
            Title = title,
            Kind = kind,
            Color = "#3EC6FF",
            CategoriesFormula = $"{sheet}!$A$2:$A$3",
            Categories = ["x", "y"],
            Series = series,
            NumericCategories = kind == CxChartKind.Scatter
        };
    }

    static string TempXlsx()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumClosedXmlTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "book.xlsx");
    }
}
