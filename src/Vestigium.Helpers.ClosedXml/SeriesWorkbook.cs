using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.ClosedXml;

internal static class SeriesWorkbook
{
    public static void Write(WorkbookSession book, NumericSeries series, string? prefix, int? populationSize)
    {
        HelperGuard.NotNull(book, nameof(book));
        HelperGuard.NotNull(series, nameof(series));

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

    private static void AddCharts(WorkbookSession book, NumericSeries series, string? prefix, int? populationSize)
    {
        var style = book.TableStyle;
        var chartsName = Name(prefix, "Charts");
        var histName = Name(prefix, "Histogram");
        var bandsName = Name(prefix, "Bands");
        var sampleName = Name(prefix, "Sample");
        var means = MeanIntervals(series, populationSize);

        book.Sheet(chartsName).WriteTable(
            SheetTable.Create(
                ["Level", "Estimate", "Lower", "Upper"],
                means.Select(m => (IReadOnlyList<object?>)[m.Level, m.Estimate, m.Lower, m.Upper]),
                "Charts"),
            AnalyticsOptions("5A6F8C", style));

        var hist = series.Full.Frequency.Histogram;
        if (hist.Count > 0)
        {
            var last = 1 + hist.Count;
            var histogram = new SheetChart
            {
                Sheet = chartsName,
                Title = "Histogram",
                Kind = ChartKind.Column,
                CategoriesFormula = ExcelNames.A1Range(histName, 2, 2, 2, last),
                Categories = hist.Select(b => ((double)b.LowerInclusive).ToString("G6")).ToArray(),
                NumericCategories = true,
                Series =
                [
                    new ChartSeries
                    {
                        Name = "Count",
                        ValuesFormula = ExcelNames.A1Range(histName, 5, 2, 5, last),
                        Values = hist.Select(b => (double)b.Count).ToArray(),
                        Color = "4C6B8A"
                    }
                ],
                FromColumn = 5,
                FromRow = 1,
                ToColumn = 14,
                ToRow = 16,
                Color = "4C6B8A"
            };
            book.AddChart(histogram);
            book.AddChart(new SheetChart
            {
                Sheet = histName,
                Title = histogram.Title,
                Kind = histogram.Kind,
                CategoriesFormula = histogram.CategoriesFormula,
                Categories = histogram.Categories,
                NumericCategories = true,
                Series = histogram.Series,
                FromColumn = 7,
                FromRow = 1,
                ToColumn = 16,
                ToRow = 18,
                Color = "4C6B8A"
            });
        }

        var bandRows = series.Bands;
        if (bandRows.Count > 0)
        {
            var last = 1 + bandRows.Count;
            book.AddChart(new SheetChart
            {
                Sheet = chartsName,
                Title = "Band means",
                Kind = ChartKind.Column,
                CategoriesFormula = ExcelNames.A1Range(bandsName, 1, 2, 1, last),
                Categories = bandRows.Select(b => b.Kind.ToString()).ToArray(),
                Series =
                [
                    new ChartSeries
                    {
                        Name = "Mean",
                        ValuesFormula = ExcelNames.A1Range(bandsName, 6, 2, 6, last),
                        Values = bandRows.Select(b => b.Mean ?? 0d).ToArray(),
                        Color = "2A6F97"
                    }
                ],
                FromColumn = 15,
                FromRow = 1,
                ToColumn = 24,
                ToRow = 16,
                Color = "2A6F97"
            });
        }

        if (means.Count > 0)
        {
            var last = 1 + means.Count;
            book.AddChart(new SheetChart
            {
                Sheet = chartsName,
                Title = "Mean confidence",
                Kind = ChartKind.Column,
                CategoriesFormula = ExcelNames.A1Range(chartsName, 1, 2, 1, last),
                Categories = means.Select(m => m.Level).ToArray(),
                Series =
                [
                    new ChartSeries
                    {
                        Name = "Estimate",
                        ValuesFormula = ExcelNames.A1Range(chartsName, 2, 2, 2, last),
                        Values = means.Select(m => m.Estimate).ToArray(),
                        Color = "3E8E7E"
                    },
                    new ChartSeries
                    {
                        Name = "Lower",
                        ValuesFormula = ExcelNames.A1Range(chartsName, 3, 2, 3, last),
                        Values = means.Select(m => m.Lower).ToArray(),
                        Color = "2A6F97"
                    },
                    new ChartSeries
                    {
                        Name = "Upper",
                        ValuesFormula = ExcelNames.A1Range(chartsName, 4, 2, 4, last),
                        Values = means.Select(m => m.Upper).ToArray(),
                        Color = "1F4E79"
                    }
                ],
                FromColumn = 5,
                FromRow = 18,
                ToColumn = 14,
                ToRow = 34,
                Color = "3E8E7E"
            });
        }

        var n = series.Count;
        if (n > 0)
        {
            book.AddChart(new SheetChart
            {
                Sheet = chartsName,
                Title = "Sample",
                Kind = ChartKind.Line,
                CategoriesFormula = ExcelNames.A1Range(sampleName, 1, 2, 1, 1 + n),
                Categories = Enumerable.Range(0, n).Select(i => i.ToString()).ToArray(),
                NumericCategories = true,
                Series =
                [
                    new ChartSeries
                    {
                        Name = "Value",
                        ValuesFormula = ExcelNames.A1Range(sampleName, 2, 2, 2, 1 + n),
                        Values = series.Values.Select(v => (double)v).ToArray(),
                        Color = "3D4F66"
                    }
                ],
                FromColumn = 15,
                FromRow = 18,
                ToColumn = 24,
                ToRow = 34,
                Color = "3D4F66"
            });
            book.AddChart(new SheetChart
            {
                Sheet = chartsName,
                Title = "Sample scatter",
                Kind = ChartKind.Scatter,
                CategoriesFormula = ExcelNames.A1Range(sampleName, 1, 2, 1, 1 + n),
                Categories = Enumerable.Range(0, n).Select(i => i.ToString()).ToArray(),
                NumericCategories = true,
                Series =
                [
                    new ChartSeries
                    {
                        Name = "Value",
                        ValuesFormula = ExcelNames.A1Range(sampleName, 2, 2, 2, 1 + n),
                        Values = series.Values.Select(v => (double)v).ToArray(),
                        Color = "5B9BD5"
                    }
                ],
                FromColumn = 25,
                FromRow = 18,
                ToColumn = 34,
                ToRow = 34,
                Color = "5B9BD5"
            });
        }

        if (hist.Count > 0)
        {
            var last = 1 + hist.Count;
            book.AddChart(new SheetChart
            {
                Sheet = chartsName,
                Title = "Histogram share",
                Kind = ChartKind.Pie,
                CategoriesFormula = ExcelNames.A1Range(histName, 2, 2, 2, last),
                Categories = hist.Select(b => ((double)b.LowerInclusive).ToString("G6")).ToArray(),
                Series =
                [
                    new ChartSeries
                    {
                        Name = "Count",
                        ValuesFormula = ExcelNames.A1Range(histName, 5, 2, 5, last),
                        Values = hist.Select(b => (double)b.Count).ToArray(),
                        Color = "1F4E79"
                    }
                ],
                FromColumn = 25,
                FromRow = 1,
                ToColumn = 34,
                ToRow = 16,
                Color = "1F4E79"
            });
        }
    }

    private static List<(string Level, double Estimate, double Lower, double Upper)> MeanIntervals(
        NumericSeries series,
        int? populationSize)
    {
        var list = new List<(string, double, double, double)>(4);
        void Add(string label, ConfidenceInterval iv)
        {
            if (!iv.IsDefined)
                return;
            list.Add((label, iv.Estimate ?? 0, iv.Lower ?? 0, iv.Upper ?? 0));
        }

        Add("90%", series.Confidence(0.90).Mean);
        Add("95%", series.Confidence(0.95).Mean);
        Add("99%", series.Confidence(0.99).Mean);
        if (populationSize is { } n)
            Add("95% FPC", series.Confidence(0.95, n).Mean);
        return list;
    }

    private static SheetWriteOptions AnalyticsOptions(string tab, string? tableStyle)
        => new()
        {
            CreateExcelTable = true,
            NumberFormat = "0.0000",
            DateFormat = "yyyy-mm-dd hh:mm:ss",
            TabColor = tab,
            TableStyle = tableStyle
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
