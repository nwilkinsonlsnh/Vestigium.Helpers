using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.ClosedXml;

internal static class SeriesWorkbook
{
    public static void Write(WorkbookSession book, NumericSeries series, string? prefix, int? populationSize)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(series);

        var full = series.Full;
        book.Sheet(Name(prefix, "Summary")).WriteTable(Summary(series), AnalyticsOptions("1F4E79"));
        book.Sheet(Name(prefix, "Bands")).WriteTable(Bands(series), AnalyticsOptions("2A6F97"));
        book.Sheet(Name(prefix, "Confidence")).WriteTable(Confidence(series, populationSize), AnalyticsOptions("3E8E7E"));
        book.Sheet(Name(prefix, "Histogram")).WriteTable(Histogram(full), AnalyticsOptions("4C6B8A"));
        book.Sheet(Name(prefix, "Sample")).WriteTable(Sample(series), new SheetWriteOptions
        {
            CreateExcelTable = true,
            DateFormat = "yyyy-mm-dd hh:mm:ss",
            TabColor = "3D4F66"
        });
    }

    private static SheetWriteOptions AnalyticsOptions(string tab)
        => new()
        {
            CreateExcelTable = true,
            NumberFormat = "0.0000",
            DateFormat = "yyyy-mm-dd hh:mm:ss",
            TabColor = tab
        };

    private static string Name(string? prefix, string leaf)
        => string.IsNullOrWhiteSpace(prefix) ? leaf : ExcelNames.Sanitize($"{prefix.Trim()} {leaf}");

    private static SheetTable Summary(NumericSeries series)
    {
        var full = series.Full;
        (string Key, object? Value)[] pairs =
        [
            ("Name", series.Name ?? ""),
            ("n", series.Count),
            ("Min", full.Min),
            ("Q1", full.Q1),
            ("Median", full.Median),
            ("Q3", full.Q3),
            ("Max", full.Max),
            ("Range", full.Range),
            ("IQR", full.Iqr),
            ("Mean", full.Mean),
            ("StdDev", full.StdDev),
            ("SEM", full.StandardErrorOfMean),
            ("CV", full.CoefficientOfVariation),
            ("Skewness", full.Skewness),
            ("ExcessKurtosis", full.ExcessKurtosis),
            ("P90", full.Percentile(0.90)),
            ("P95", full.Percentile(0.95)),
            ("P99", full.Percentile(0.99)),
            ("LowOutliers", full.LowOutliers.Count),
            ("HighOutliers", full.HighOutliers.Count),
            ("GeometricMean", full.GeometricMean),
            ("HarmonicMean", full.HarmonicMean)
        ];
        return SheetTable.KeyValue("Metric", "Value", pairs, "Summary");
    }

    private static SheetTable Bands(NumericSeries series)
    {
        var rows = series.Bands.Select(b => (IReadOnlyList<object?>)[
            b.Kind.ToString(),
            b.Count,
            b.Min,
            b.Median,
            b.Max,
            b.Mean,
            b.StdDev,
            b.Skewness,
            b.ExcessKurtosis,
            b.Range,
            b.Iqr
        ]);
        return SheetTable.Create(
            ["Band", "n", "Min", "P50", "Max", "Mean", "StdDev", "Skew", "ExKurt", "Range", "IQR"],
            rows,
            "Bands");
    }

    private static SheetTable Confidence(NumericSeries series, int? populationSize)
    {
        var rows = new List<IReadOnlyList<object?>>();
        void Add(ConfidenceReport report, string label)
        {
            AddInterval(rows, label, report.Mean);
            AddInterval(rows, label, report.Median);
            AddInterval(rows, label, report.Variance);
            AddInterval(rows, label, report.StdDev);
        }

        Add(series.Confidence(0.90), "90%");
        Add(series.Confidence(0.95), "95%");
        Add(series.Confidence(0.99), "99%");
        if (populationSize is { } n)
            Add(series.Confidence(0.95, n), "95% FPC");

        return SheetTable.Create(
            ["Level", "Parameter", "Estimate", "Lower", "Upper", "Width", "Method", "Defined"],
            rows,
            "Confidence");
    }

    private static void AddInterval(List<IReadOnlyList<object?>> rows, string level, ConfidenceInterval iv)
    {
        rows.Add([
            level,
            iv.Parameter,
            iv.Estimate,
            iv.Lower,
            iv.Upper,
            iv.Width,
            iv.Method,
            iv.IsDefined
        ]);
    }

    private static SheetTable Histogram(SeriesSlice full)
    {
        var rows = full.Frequency.Histogram.Select((b, i) => (IReadOnlyList<object?>)[
            i,
            b.LowerInclusive,
            b.UpperInclusive,
            b.UpperIsClosed,
            b.Count,
            b.RelativeFrequency
        ]);
        return SheetTable.Create(
            ["Index", "Lower", "Upper", "Closed", "Count", "Relative"],
            rows,
            "Histogram");
    }

    private static SheetTable Sample(NumericSeries series)
    {
        var rows = new List<IReadOnlyList<object?>>(series.Count);
        for (var i = 0; i < series.Count; i++)
        {
            object? at = series.HasTimestamps && i < series.Times.Count ? series.Times[i] : null;
            rows.Add([i, series.Values[i], at]);
        }

        return SheetTable.Create(["Index", "Value", "Timestamp"], rows, "Sample");
    }
}
