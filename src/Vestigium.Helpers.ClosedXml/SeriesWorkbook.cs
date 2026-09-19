using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.ClosedXml;

internal static class SeriesWorkbook
{
    public static void Write(WorkbookSession book, NumericSeries series, string? prefix, int? populationSize)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(series);

        var full = series.Full;
        var style = book.TableStyle;
        book.Sheet(Name(prefix, "Summary")).WriteTable(Summary(series), AnalyticsOptions("1F4E79", style));
        book.Sheet(Name(prefix, "Bands")).WriteTable(Bands(series), AnalyticsOptions("2A6F97", style));
        book.Sheet(Name(prefix, "Confidence")).WriteTable(Confidence(series, populationSize), AnalyticsOptions("3E8E7E", style));
        book.Sheet(Name(prefix, "Histogram")).WriteTable(Histogram(full), AnalyticsOptions("4C6B8A", style));
        book.Sheet(Name(prefix, "Sample")).WriteTable(Sample(series), new SheetWriteOptions
        {
            CreateExcelTable = true,
            DateFormat = "yyyy-mm-dd hh:mm:ss",
            TabColor = "3D4F66",
            TableStyle = style,
            HighlightColumn = "Value",
            HighlightGreaterThan = (double)series.Full.Percentile(0.95)
        });
        if (book.IncludeCharts)
            AddCharts(book, series, prefix, populationSize);

        if (book.IncludeCharts)
            book.ReorderSheets(
                Name(prefix, "Summary"),
                Name(prefix, "Charts"),
                Name(prefix, "Bands"),
                Name(prefix, "Confidence"),
                Name(prefix, "Histogram"),
                Name(prefix, "Sample"));
        else
            book.ReorderSheets(
                Name(prefix, "Summary"),
                Name(prefix, "Bands"),
                Name(prefix, "Confidence"),
                Name(prefix, "Histogram"),
                Name(prefix, "Sample"));
    }
