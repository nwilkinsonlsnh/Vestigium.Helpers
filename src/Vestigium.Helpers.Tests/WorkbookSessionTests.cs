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
        Assert.Contains("h", cell.Style.NumberFormat.Format, StringComparison.OrdinalIgnoreCase);
        // ClosedXML 0.105 rehydrates [h]:mm:ss as TimeSpan on open; the write
        // path stored TotalDays as a number. Either type is the same day count.
        var days = cell.DataType == XLDataType.TimeSpan
            ? cell.GetTimeSpan().TotalDays
            : cell.GetDouble();
        Assert.Equal(TimeSpan.FromMinutes(90).TotalDays, days, 12);
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

    [Fact]
    public void Table_style_preview_matches_the_excel_gallery()
    {
        Assert.Equal(60, ExcelTableStylePreview.All.Count);
        Assert.Equal(21, ExcelTableStylePreview.In("Light").Count);
        Assert.Equal(28, ExcelTableStylePreview.In("Medium").Count);
        Assert.Equal(11, ExcelTableStylePreview.In("Dark").Count);
        var medium2 = ExcelTableStylePreview.Of("Medium2");
        Assert.Equal("Medium", medium2.Group);
        Assert.Equal(2, medium2.Index);
        Assert.Equal("TableStyleMedium2", medium2.ExcelName);
        Assert.Equal("#ED7D31", medium2.Header);
        var light1 = ExcelTableStylePreview.Of("Light1");
        Assert.Equal("#FFFFFF", light1.Header);
        Assert.Equal("#2F5496", light1.HeaderInk);
    }

    [Fact]
    public void ReadUsedRange_round_trips_types()
    {
        var path = TempXlsx();
        var at = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Unspecified);
        using (var book = WorkbookHelper.Create("Data", "ClosedXml"))
        {
            book.Sheet("Data").WriteTable(SheetTable.Create(
                ["Name", "Value", "Flag", "When"],
                [
                    ["alpha", 1.5, true, at],
                    ["beta", 2, false, at.AddHours(1)]
                ]));
            book.SaveAs(path);
        }

        using var reopen = WorkbookHelper.Open(path, "ClosedXml");
        var table = reopen.Sheet("Data").ReadUsedRange();
        Assert.Equal(["Name", "Value", "Flag", "When"], table.Headers);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("alpha", table.Rows[0][0]);
        Assert.Equal(1.5, Convert.ToDouble(table.Rows[0][1], System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(true, table.Rows[0][2]);
        var when = Assert.IsType<DateTime>(table.Rows[0][3]);
        Assert.Equal(at, when, TimeSpan.FromSeconds(1));
        Assert.Equal("text", SheetTable.CellKind(table.Rows[0][0]));
        Assert.Equal("number", SheetTable.CellKind(table.Rows[0][1]));
        Assert.Equal("bool", SheetTable.CellKind(table.Rows[0][2]));
        Assert.Equal("datetime", SheetTable.CellKind(table.Rows[0][3]));
    }

    [Fact]
    public void ReadUsedRange_round_trips_the_analytics_shape()
    {
        var path = TempXlsx();
        var series = NumericSeries.From(Enumerable.Range(1, 9), "odd");
        using (var book = WorkbookHelper.Create("Summary", "ClosedXml"))
        {
            book.IncludeCharts = false;
            WorkbookHelper.WriteSeries(book, series, populationSize: 9);
            book.SaveAs(path);
        }

        using var reopen = WorkbookHelper.Open(path, "ClosedXml");
        var summary = reopen.Sheet("Summary").ReadUsedRange();
        Assert.Equal(22, summary.Rows.Count);
        Assert.Equal("n", summary.Rows[1][0]);
        Assert.Equal(9d, Convert.ToDouble(summary.Rows[1][1], System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("odd", summary.Rows[0][1]);
        var hist = reopen.Sheet("Histogram").ReadUsedRange();
        Assert.Equal(series.Full.Frequency.Histogram.Count, hist.Rows.Count);
        var sample = reopen.Sheet("Sample").ReadUsedRange();
        Assert.Equal(9, sample.Rows.Count);
        Assert.Equal(1d, Convert.ToDouble(sample.Rows[0][1], System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(9d, Convert.ToDouble(sample.Rows[8][1], System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Empty_sheet_reads_as_an_empty_table()
    {
        using var book = WorkbookHelper.Create("Blank", "ClosedXml");
        var table = book.Sheet("Blank").ReadUsedRange();
        Assert.Empty(table.Headers);
        Assert.Empty(table.Rows);
    }

    [Fact]
    public void Operator_print_is_landscape_fit_to_width_with_appid_footer()
    {
        var path = TempXlsx();
        using (var book = WorkbookHelper.Create("Data", "ClosedXml"))
        {
            book.Sheet("Data").WriteTable(SheetTable.Create(["ms"], [[12.5]]));
            book.SaveAs(path);
        }

        using var wb = new XLWorkbook(path);
        var setup = wb.Worksheet("Data").PageSetup;
        Assert.Equal(XLPageOrientation.Landscape, setup.PageOrientation);
        Assert.Equal(1, setup.PagesWide);
        var footer = setup.Footer.Left.GetText(XLHFOccurrence.OddPages);
        if (string.IsNullOrEmpty(footer))
            footer = setup.Footer.Left.GetText(XLHFOccurrence.AllPages);
        Assert.Contains("ClosedXml", footer, StringComparison.Ordinal);
    }

    [Fact]
    public void Header_name_ms_pct_utc_gets_a_number_format()
    {
        var path = TempXlsx();
        var at = new DateTime(2026, 9, 8, 16, 0, 0);
        using (var book = WorkbookHelper.Create("Fmt", "ClosedXml"))
        {
            book.Sheet("Fmt").WriteTable(SheetTable.Create(
                ["ms", "pct", "utc"],
                [[12.5, 0.1234, at]]));
            book.SaveAs(path);
        }

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("Fmt");
        Assert.Equal("0.0", ws.Cell(2, 1).Style.NumberFormat.Format);
        Assert.Contains("%", ws.Cell(2, 2).Style.NumberFormat.Format, StringComparison.Ordinal);
        var dateFmt = ws.Cell(2, 3).Style.DateFormat.Format;
        if (string.IsNullOrWhiteSpace(dateFmt))
            dateFmt = ws.Cell(2, 3).Style.NumberFormat.Format;
        Assert.Contains("yy", dateFmt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Highlight_column_adds_one_greater_than_rule()
    {
        var path = TempXlsx();
        using (var book = WorkbookHelper.Create("Data", "ClosedXml"))
        {
            book.Sheet("Data").WriteTable(
                SheetTable.Create(["Name", "Value"], [["a", 1], ["b", 99]]),
                new SheetWriteOptions { HighlightColumn = "Value", HighlightGreaterThan = 50 });
            book.SaveAs(path);
        }

        using var wb = new XLWorkbook(path);
        var formats = wb.Worksheet("Data").ConditionalFormats;
        Assert.Single(formats);
        var cf = formats.Single();
        Assert.Equal(2, cf.Range.RangeAddress.FirstAddress.ColumnNumber);
    }

    [Fact]
    public void ReorderSheets_puts_named_tabs_first()
    {
        using var book = WorkbookHelper.Create("A", "ClosedXml");
        book.AddSheet("B");
        book.AddSheet("C");
        book.ReorderSheets("C", "A");
        Assert.Equal(new[] { "C", "A", "B" }, book.SheetNames);
        book.MoveSheet("B", 1);
        Assert.Equal("B", book.SheetNames[0]);
    }

    [Fact]
    public void MoveSheet_missing_name_throws()
    {
        using var book = WorkbookHelper.Create("A", "ClosedXml");
        Assert.Throws<KeyNotFoundException>(() => book.MoveSheet("Nope", 1));
    }

    [Fact]
    public void WriteSeries_highlights_sample_values_above_p95()
    {
        var path = TempXlsx();
        var series = NumericSeries.From(Enumerable.Range(1, 9), "odd");
        using (var book = WorkbookHelper.Create("Summary", "ClosedXml"))
        {
            book.IncludeCharts = false;
            WorkbookHelper.WriteSeries(book, series, populationSize: 9);
            book.SaveAs(path);
        }

        using var wb = new XLWorkbook(path);
        Assert.NotEmpty(wb.Worksheet("Sample").ConditionalFormats);
    }

    [Fact]
    public void WriteNamedRange_fills_letterhead_without_wiping_banner()
    {
        var path = TempXlsx();
        using (var letterhead = WorkbookHelper.Create("Letterhead", "ClosedXml"))
        {
            letterhead.Sheet("Letterhead").WriteAt(
                1, 1,
                SheetTable.Create(["Banner"], [["VESTIGIUM"]]),
                SheetWriteOptions.Letterhead);
            letterhead.DefineName("Data", "Letterhead", 5, 1, 8, 2);
            letterhead.SaveAs(path);
        }

        using (var book = WorkbookHelper.OpenTemplate(path, "ClosedXml"))
        {
            Assert.Contains("Data", book.NamedRanges);
            book.WriteNamedRange("Data", SheetTable.Create(
                ["Name", "Value"],
                [["alpha", 1], ["beta", 2]]));
            book.Save();
        }

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("Letterhead");
        Assert.Equal("Banner", ws.Cell(1, 1).GetString());
        Assert.Equal("VESTIGIUM", ws.Cell(2, 1).GetString());
        Assert.Equal("Name", ws.Cell(5, 1).GetString());
        Assert.Equal("alpha", ws.Cell(6, 1).GetString());
        Assert.Equal(1d, ws.Cell(6, 2).GetDouble());
        Assert.Equal("beta", ws.Cell(7, 1).GetString());
        var defined = wb.DefinedName("Data") ?? throw new InvalidOperationException("Data name missing.");
        Assert.Contains("$A$5", defined.RefersTo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WriteNamedRange_missing_name_throws()
    {
        using var book = WorkbookHelper.Create("Letterhead", "ClosedXml");
        Assert.Throws<KeyNotFoundException>(() =>
            book.WriteNamedRange("Data", SheetTable.Create(["X"], [[1]])));
    }

    [Fact]
    public void AddPicture_embeds_a_media_part()
    {
        var path = TempXlsx();
        var png = TinyPngPath();
        using (var book = WorkbookHelper.Create("Letterhead", "ClosedXml"))
        {
            book.Sheet("Letterhead").WriteAt(
                1, 1,
                SheetTable.Create(["Title"], [["Logo"]]),
                SheetWriteOptions.Letterhead);
            book.AddPicture("Letterhead", png, row: 1, column: 3, widthPx: 120, heightPx: 36, name: "Logo");
            book.SaveAs(path);
        }

        using var zip = ZipFile.OpenRead(path);
        var names = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToArray();
        Assert.Contains(names, n => n.StartsWith("xl/media/", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, n => n.Contains("drawing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Merge_appends_matching_sheets_and_copies_unknown_ones()
    {
        var targetPath = TempXlsx();
        using (var target = WorkbookHelper.Create("Log", "ClosedXml"))
        {
            target.Sheet("Log").WriteTable(SheetTable.Create(["Id"], [[1]]));
            target.AddSheet("Keep");
            target.Sheet("Keep").WriteTable(SheetTable.Create(["Note"], [["stay"]]));
            target.SaveAs(targetPath);
        }

        using var source = WorkbookHelper.Create("Log", "ClosedXml");
        source.Sheet("Log").WriteTable(SheetTable.Create(["Id"], [[2], [3]]));
        source.Sheet("Extra").WriteTable(SheetTable.Create(["X"], [["new"]]));

        using (var target = WorkbookHelper.Open(targetPath, "ClosedXml"))
        {
            WorkbookHelper.Merge(target, source);
            target.Save();
            Assert.Contains("Keep", target.SheetNames);
            Assert.Contains("Extra", target.SheetNames);
        }

        using var wb = new XLWorkbook(targetPath);
        var log = wb.Worksheet("Log");
        Assert.Equal(1d, log.Cell(2, 1).GetDouble());
        Assert.Equal(2d, log.Cell(3, 1).GetDouble());
        Assert.Equal(3d, log.Cell(4, 1).GetDouble());
        Assert.Equal("stay", wb.Worksheet("Keep").Cell(2, 1).GetString());
        Assert.Equal("new", wb.Worksheet("Extra").Cell(2, 1).GetString());
    }

    [Fact]
    public void Merge_into_self_throws()
    {
        using var book = WorkbookHelper.Create("A", "ClosedXml");
        Assert.Throws<ArgumentException>(() => book.Merge(book));
    }

    [Fact]
    public void WriteSeries_embeds_pie_and_scatter_charts()
    {
        var path = TempXlsx();
        var series = NumericSeries.From(Enumerable.Range(1, 9), "odd");
        using (var book = WorkbookHelper.Create("Summary", "ClosedXml"))
        {
            WorkbookHelper.WriteSeries(book, series, populationSize: 9);
            Assert.Contains(book.Charts, c => c.Kind == ChartKind.Pie);
            Assert.Contains(book.Charts, c => c.Kind == ChartKind.Scatter);
            book.SaveAs(path);
        }

        using var zip = ZipFile.OpenRead(path);
        var xml = new List<string>();
        foreach (var entry in zip.Entries)
        {
            var name = entry.FullName.Replace('\\', '/');
            if (!name.StartsWith("xl/charts/chart", StringComparison.OrdinalIgnoreCase))
                continue;
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            xml.Add(reader.ReadToEnd());
        }

        Assert.Contains(xml, x => x.Contains("c:pieChart", StringComparison.Ordinal));
        Assert.Contains(xml, x => x.Contains("c:scatterChart", StringComparison.Ordinal));
    }

    [Fact]
    public void SheetChrome_default_is_bold_freeze_filter_print()
    {
        var chrome = SheetChrome.Default;
        Assert.True(chrome.BoldHeader);
        Assert.True(chrome.FreezeHeader);
        Assert.True(chrome.AutoFilter);
        Assert.True(chrome.OperatorPrint);
        Assert.Null(chrome.TabColor);
    }

    [Fact]
    public void ApplyChrome_on_empty_sheet_sets_tab_and_print()
    {
        var path = TempXlsx();
        using (var book = WorkbookHelper.Create("Blank", "ClosedXml"))
        {
            book.Sheet("Blank").ApplyChrome(new SheetChrome
            {
                TabColor = "#112233",
                OperatorPrint = true
            });
            book.SaveAs(path);
        }

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("Blank");
        Assert.Equal(XLColor.FromHtml("#112233"), ws.TabColor);
        Assert.Equal(XLPageOrientation.Landscape, ws.PageSetup.PageOrientation);
    }

    [Fact]
    public void ApplyChrome_on_written_sheet_freezes_filters_and_can_skip_chrome()
    {
        var path = TempXlsx();
        using (var book = WorkbookHelper.Create("Data", "ClosedXml"))
        {
            book.Sheet("Data").WriteTable(
                SheetTable.Create(["Name", "Value"], [["a", 1], ["b", 2]]),
                new SheetWriteOptions { CreateExcelTable = false, Autosize = false, FreezeHeader = false, AutoFilter = false, OperatorPrint = false });
            book.Sheet("Data").ApplyChrome(new SheetChrome
            {
                BoldHeader = true,
                FreezeHeader = true,
                AutoFilter = true,
                TabColor = "#3EC6FF",
                OperatorPrint = true
            });
            book.SaveAs(path);
        }

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("Data");
        Assert.True(ws.SheetView.SplitRow >= 1);
        Assert.True(ws.AutoFilter.IsEnabled);
        Assert.Equal(XLColor.FromHtml("#3EC6FF"), ws.TabColor);
        Assert.True(ws.Cell(1, 1).Style.Font.Bold);
    }

    [Fact]
    public void ApplyChrome_can_turn_features_off()
    {
        using var book = WorkbookHelper.Create("Data", "ClosedXml");
        book.Sheet("Data").WriteTable(
            SheetTable.Create(["X"], [[1]]),
            new SheetWriteOptions { CreateExcelTable = false, FreezeHeader = false, AutoFilter = false, OperatorPrint = false });
        book.Sheet("Data").ApplyChrome(new SheetChrome
        {
            BoldHeader = false,
            FreezeHeader = false,
            AutoFilter = false,
            OperatorPrint = false
        });
        Assert.Equal("Data", book.Sheet("Data").Name);
    }

    [Fact]
    public void WriteTable_covers_numeric_date_guid_and_null_cells()
    {
        var path = TempXlsx();
        var utc = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        using (var book = WorkbookHelper.Create("Types", "ClosedXml"))
        {
            book.Sheet("Types").WriteTable(SheetTable.Create(
                ["Byte", "Short", "ULong", "Dec", "When", "Id", "Blank"],
                [
                    [(byte)7, (short)-2, ((ulong)long.MaxValue) + 1UL, 1.25m, utc, guid, null]
                ]),
                new SheetWriteOptions { CreateExcelTable = false, Autosize = false });
            book.SaveAs(path);
        }

        using var reopen = WorkbookHelper.Open(path, "ClosedXml");
        var table = reopen.Sheet("Types").ReadUsedRange();
        Assert.Equal(7d, Convert.ToDouble(table.Rows[0][0], System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(-2d, Convert.ToDouble(table.Rows[0][1], System.Globalization.CultureInfo.InvariantCulture));
        Assert.Null(table.Rows[0][6]);
        Assert.Equal("blank", SheetTable.CellKind(null));
    }

    [Fact]
    public void Highlight_on_empty_sheet_throws()
    {
        using var book = WorkbookHelper.Create("Blank", "ClosedXml");
        var empty = Assert.Throws<InvalidOperationException>(() => book.Sheet("Blank").HighlightGreaterThan("X", 1));
        Assert.Contains("no used range", empty.Message, StringComparison.Ordinal);
        book.Sheet("Blank").WriteTable(SheetTable.Create(["X"], [[1]]));
        var missing = Assert.Throws<ArgumentException>(() => book.Sheet("Blank").HighlightGreaterThan("Nope", 1));
        Assert.Contains("Nope", missing.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SheetTable_create_rejects_empty_headers()
        => Assert.Throws<ArgumentException>(() => SheetTable.Create([], [[1]]));

    [Fact]
    public void RemoveSheet_keeps_one_worksheet()
    {
        using var book = WorkbookHelper.Create("A", "ClosedXml");
        book.AddSheet("B");
        Assert.True(book.RemoveSheet("B"));
        Assert.False(book.RemoveSheet("Nope"));
        var last = Assert.Throws<InvalidOperationException>(() => book.RemoveSheet("A"));
        Assert.Contains("at least one worksheet", last.Message, StringComparison.Ordinal);
    }

    private static string TinyPngPath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "logo.png");
        File.WriteAllBytes(path, Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPj/HwADBwGAtKqTqQAAAABJRU5ErkJggg=="));
        return path;
    }

    private static string TempXlsx()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "book.xlsx");
    }
}
