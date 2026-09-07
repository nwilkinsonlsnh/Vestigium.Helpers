using System.Globalization;
using System.Security.Cryptography;
using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;
using Vestigium.Logging;

return HelperDemoHost.Run(
    HelperLog.AppIds.Analytics,
    AnalyticsHelper.Identity,
    AnalyticsDemo.Run);

internal static class AnalyticsDemo
{
    private const int SampleSize = 1000;
    private const int PopulationSize = 100_000;
    private const double Level = 0.95;

    public static void Run()
    {
        var app = HelperLog.AppIds.Analytics;
        HelperLog.Information(app, VestigiumStatus.Pending, app,
            $"Drawing {SampleSize} unique integers from 1..{PopulationSize} with RandomNumberGenerator.");

        var origin = new DateTimeOffset(2026, 9, 7, 16, 0, 0, TimeSpan.Zero);
        var observations = DrawUniqueCryptoSample(SampleSize, PopulationSize, origin);
        var series = NumericSeries.FromObservations(observations, "crypto-1k");
        var full = series.Full;
        var ci95 = series.Confidence(Level);
        var ci90 = series.Confidence(0.90);
        var ci99 = series.Confidence(0.99);
        var ciFpc = series.Confidence(Level, PopulationSize);
        var hypothesizedMean = (PopulationSize + 1) / 2d;
        var pValue = series.MeanPValue(hypothesizedMean);
        var covering = series.MeanConfidenceLevelContaining(hypothesizedMean);
        var nForMargin = series.SampleSizeForMeanMargin(targetMargin: 1_000, Level);
        var abovePopMedian = series.ProportionAbove((decimal)hypothesizedMean, Level);
        var atLeastP95 = series.ProportionAtLeast(full.Percentile(0.95), Level);

        var mid = origin.AddSeconds(SampleSize / 2);
        var firstHalf = series.Slice(origin, mid, "first-500s");
        var secondHalf = series.Slice(mid, origin.AddSeconds(SampleSize + 1), "last-500s");

        WriteBanner(series, origin);
        WriteSlice("FULL SERIES", full);
        WriteNamedPercentiles(full);
        WriteBands(series);
        WriteFrequency(full);
        WriteHistogram(full);
        WriteConfidence("90%", ci90);
        WriteConfidence("95%  (default)", ci95);
        WriteConfidence("99%", ci99);
        WriteConfidence($"95%  with FPC  N={PopulationSize:N0}", ciFpc);
        WriteInterval("Wilson  P(X > pop median)", abovePopMedian);
        WriteInterval("Wilson  P(X ≥ sample P95)", atLeastP95);
        Blank();
        WriteHeading("INFERENCE  (H0: mean = population mean of 1..N)");
        WriteKv("hypothesized mean", hypothesizedMean);
        WriteKv("two-sided t p-value", pValue);
        WriteKv("just-covering γ  (1 − p)", covering);
        WriteKv("n for mean margin ±1000 at 95%", nForMargin);
        Blank();
        WriteHeading("TIME SLICE");
        WriteKv("window", $"{series.Window.Kind}  [{FmtTime(series.FirstAt)} .. {FmtTime(series.LastAt)}]");
        WriteKv("first-500s  n / mean / P50", $"{firstHalf.Count} / {FmtNum(firstHalf.Full.Mean)} / {FmtDec(firstHalf.Full.Median)}");
        WriteKv("last-500s   n / mean / P50", $"{secondHalf.Count} / {FmtNum(secondHalf.Full.Mean)} / {FmtDec(secondHalf.Full.Median)}");
        Blank();
        WriteChartReady(series);

        HelperLog.Information(
            app,
            VestigiumStatus.Success,
            app,
            $"n={series.Count} unique={full.Frequency.DistinctCount} mean={full.Mean:F2} P50={full.Median} P90={full.Percentile(0.90)} P95={full.Percentile(0.95)} P99={full.Percentile(0.99)} skew={full.Skewness:F4} excessKurt={full.ExcessKurtosis:F4} Q4={series.Q4.Count} highOut={full.HighOutliers.Count} meanCI=[{ci95.Mean.Lower:F2},{ci95.Mean.Upper:F2}] fpcCI=[{ciFpc.Mean.Lower:F2},{ciFpc.Mean.Upper:F2}] p={pValue:F4} Identity={AnalyticsHelper.Identity}");
    }

    private static List<Observation> DrawUniqueCryptoSample(int count, int populationSize, DateTimeOffset origin)
    {
        var drawn = new HashSet<int>(count);
        while (drawn.Count < count)
            drawn.Add(RandomNumberGenerator.GetInt32(1, populationSize + 1));

        var values = drawn.ToArray();
        for (var i = values.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }

        var observations = new List<Observation>(count);
        for (var i = 0; i < values.Length; i++)
            observations.Add(new Observation(values[i], origin.AddSeconds(i)));
        return observations;
    }

    private static void WriteBanner(NumericSeries series, DateTimeOffset origin)
    {
        WriteHeading("SAMPLE");
        WriteKv("generator", "System.Security.Cryptography.RandomNumberGenerator (CSPRNG)");
        WriteKv("draw", $"{SampleSize:N0} unique integers without replacement");
        WriteKv("population", $"1 .. {PopulationSize:N0}   (finite N known, used only for FPC)");
        WriteKv("name", series.Name);
        WriteKv("n", series.Count);
        WriteKv("timestamps", series.HasTimestamps);
        WriteKv("first / last", $"{FmtTime(series.FirstAt)}  /  {FmtTime(series.LastAt)}");
        WriteKv("origin", FmtTime(origin));
        WriteKv("sorted head", Join(series.Sorted.Take(8)));
        WriteKv("sorted tail", Join(series.Sorted.TakeLast(8)));
        Blank();
    }

    private static void WriteSlice(string title, SeriesSlice slice)
    {
        WriteHeading(title);
        WriteKv("kind / n / empty", $"{slice.Kind}  /  {slice.Count}  /  {slice.IsEmpty}");
        WriteKv("min  Q1  median  Q3  max", $"{FmtDec(slice.Min)}  /  {FmtDec(slice.Q1)}  /  {FmtDec(slice.Median)}  /  {FmtDec(slice.Q3)}  /  {FmtDec(slice.Max)}");
        WriteKv("range / IQR", $"{FmtDec(slice.Range)}  /  {FmtDec(slice.Iqr)}");
        WriteKv("midrange / midhinge / trimean", $"{FmtDec(slice.Midrange)}  /  {FmtDec(slice.Midhinge)}  /  {FmtDec(slice.Trimean)}");
        WriteKv("Tukey fences  [low, high]", $"[{FmtDec(slice.TukeyLowerFence)}, {FmtDec(slice.TukeyUpperFence)}]");
        WriteKv("low outliers", FormatOutliers(slice.LowOutliers));
        WriteKv("high outliers (right tail)", FormatOutliers(slice.HighOutliers));
        WriteKv("all outliers", FormatOutliers(slice.Outliers));
        WriteKv("sum", FmtDec(slice.Sum));
        WriteKv("mean", FmtNum(slice.Mean));
        WriteKv("geometric mean", FmtNum(slice.GeometricMean));
        WriteKv("harmonic mean", FmtNum(slice.HarmonicMean));
        WriteKv("SSD", FmtNum(slice.SumOfSquaredDeviations));
        WriteKv("sample variance / stddev", $"{FmtNum(slice.Variance)}  /  {FmtNum(slice.StdDev)}");
        WriteKv("population variance / stddev", $"{FmtNum(slice.PopulationVariance)}  /  {FmtNum(slice.PopulationStdDev)}");
        WriteKv("SEM  (standard error of mean)", FmtNum(slice.StandardErrorOfMean));
        WriteKv("CV  (stddev / mean)", FmtNum(slice.CoefficientOfVariation));
        WriteKv("MAD (mean abs dev)", FmtNum(slice.MeanAbsoluteDeviation));
        WriteKv("MAD (median abs dev)", FmtNum(slice.MedianAbsoluteDeviation));
        WriteKv("skewness  (Excel SKEW)", FmtNum(slice.Skewness));
        WriteKv("excess kurtosis  (Excel KURT)", FmtNum(slice.ExcessKurtosis));
        WriteKv("kurtosis  (excess + 3)", FmtNum(slice.Kurtosis));
        Blank();
    }

    private static void WriteNamedPercentiles(SeriesSlice slice)
    {
        WriteHeading("NAMED PERCENTILES  (PERCENTILE.INC)  — not confidence");
        foreach (var kv in slice.NamedPercentiles())
        {
            var label = kv.Key switch
            {
                0.25 => "P25 = Q1",
                0.50 => "P50 = median",
                0.75 => "P75 = Q3",
                _ => $"P{kv.Key * 100:0}"
            };
            WriteKv(label, kv.Value);
        }

        Blank();
        Console.WriteLine("  P90/P95/P99 are rank cutoffs on this sample.");
        Console.WriteLine("  They are not 90/95/99% confidence that a parameter sits in an interval.");
        Blank();
    }

    private static void WriteBands(NumericSeries series)
    {
        WriteHeading("QUARTILE BANDS");
        Console.WriteLine($"  {"band",-6} {"n",5} {"min",10} {"P50",10} {"max",10} {"mean",12} {"stddev",12} {"skew",10} {"exKurt",10}");
        foreach (var band in series.Bands)
        {
            Console.WriteLine(
                $"  {band.Kind,-6} {band.Count,5} {FmtDec(band.Min),10} {FmtDec(band.Median),10} {FmtDec(band.Max),10} {FmtNum(band.Mean),12} {FmtNum(band.StdDev),12} {FmtNum(band.Skewness),10} {FmtNum(band.ExcessKurtosis),10}");
        }

        Blank();
        Console.WriteLine("  Q1: values ≤ series Q1     (left tail of the sample)");
        Console.WriteLine("  Q2: Q1 < v ≤ median");
        Console.WriteLine("  Q3: median < v ≤ Q3");
        Console.WriteLine("  Q4: v > series Q3          (right tail — the slow RTTs)");
        Console.WriteLine("  IQR: Q1 ≤ v ≤ Q3           (middle 50%)");
        Blank();
    }

    private static void WriteFrequency(SeriesSlice slice)
    {
        var f = slice.Frequency;
        WriteHeading("FREQUENCY");
        WriteKv("distinct values", f.DistinctCount);
        WriteKv("unique mode?", f.HasUniqueMode);
        WriteKv("mode", f.Mode is { } m ? m.ToString(CultureInfo.InvariantCulture) : "(none — every drawn value appears once)");
        WriteKv("modal values", f.Modes.Count == 0 ? "(none)" : $"{f.Modes.Count} value(s) tied at the top count");
        WriteKv("Shannon entropy (nats)", FmtNum(f.EntropyNats));
        WriteKv("top exact frequencies", Join(f.Frequencies.Take(5).Select(b => $"{b.Value}×{b.Count}")));
        Blank();
    }

    private static void WriteHistogram(SeriesSlice slice)
    {
        WriteHeading("HISTOGRAM  (Freedman–Diaconis, value axis — not time)");
        Console.WriteLine($"  {"#",3}  {"lower",12}  {"upper",12}  {"n",5}  {"rel",8}  closed?");
        var i = 0;
        foreach (var bin in slice.Frequency.Histogram)
        {
            Console.WriteLine(
                $"  {i,3}  {FmtDec(bin.LowerInclusive),12}  {FmtDec(bin.UpperInclusive),12}  {bin.Count,5}  {bin.RelativeFrequency,8:P2}  {(bin.UpperIsClosed ? "yes" : "no")}");
            i++;
        }

        Blank();
    }

    private static void WriteConfidence(string title, ConfidenceReport report)
    {
        WriteHeading($"CONFIDENCE INTERVALS  γ = {title}");
        WriteKv("chosen level", report.Level);
        WriteInterval("mean", report.Mean);
        WriteInterval("median", report.Median);
        WriteInterval("variance", report.Variance);
        WriteInterval("stddev", report.StdDev);
        Blank();
    }

    private static void WriteChartReady(NumericSeries series)
    {
        WriteHeading("CHART-READY POINTS  (numbers only — no charting in this library)");
        WriteKv("sample-order points", series.SampleOrderPoints().Count);
        WriteKv("sorted points", series.SortedPoints().Count);
        var ecdf = series.EcdfPoints();
        WriteKv("ECDF points", ecdf.Count);
        WriteKv("ECDF head", Join(ecdf.Take(3).Select(p => $"({p.X:F1}, {p.Y:F3})")));
        WriteKv("ECDF tail", Join(ecdf.TakeLast(3).Select(p => $"({p.X:F1}, {p.Y:F3})")));
        var hist = series.HistogramPoints();
        WriteKv("histogram points (count)", hist.Count);
        foreach (var p in hist)
            Console.WriteLine($"    mid={p.X,12:F2}  count={p.Y,6:F0}");
        var rel = series.HistogramRelativePoints();
        WriteKv("histogram points (relative)", rel.Count);
        var timed = series.TimeSeriesPoints();
        WriteKv("time-series points", timed.Count);
        if (timed.Count > 0)
        {
            WriteKv("time-series head", $"{FmtTime(timed[0].At)} = {timed[0].Value}");
            WriteKv("time-series tail", $"{FmtTime(timed[^1].At)} = {timed[^1].Value}");
        }

        Blank();
    }

    private static void WriteInterval(string label, ConfidenceInterval iv)
    {
        if (!iv.IsDefined)
        {
            WriteKv(label, $"undefined  ({iv.Method})");
            return;
        }

        WriteKv(
            label,
            $"est={FmtNum(iv.Estimate)}  [{FmtNum(iv.Lower)}, {FmtNum(iv.Upper)}]  width={FmtNum(iv.Width)}  {iv.Method}  γ={iv.Level:P0}");
    }

    private static void WriteHeading(string title)
    {
        Console.WriteLine(new string('=', 88));
        Console.WriteLine($"  {title}");
        Console.WriteLine(new string('=', 88));
    }

    private static void WriteKv(string key, object? value) =>
        Console.WriteLine($"  {key,-34} {value}");

    private static void Blank() => Console.WriteLine();

    private static string FormatOutliers(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
            return "(none)";
        var head = string.Join(", ", values.Take(12).Select(v => v.ToString("G", CultureInfo.InvariantCulture)));
        return values.Count <= 12 ? $"{values.Count}: {head}" : $"{values.Count}: {head}, …";
    }

    private static string Join<T>(IEnumerable<T> items) =>
        string.Join(", ", items.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)));

    private static string FmtTime(DateTimeOffset value) =>
        value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

    private static string FmtTime(DateTimeOffset? value) =>
        value is { } at ? FmtTime(at) : "—";

    private static string FmtDec(decimal value) =>
        value.ToString("G", CultureInfo.InvariantCulture);

    private static string FmtDec(decimal? value) =>
        value is { } v ? FmtDec(v) : "—";

    private static string FmtNum(double? value) =>
        value is { } v ? v.ToString("G6", CultureInfo.InvariantCulture) : "—";
}
