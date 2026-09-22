using System.Globalization;
using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.FileIo;

/// <summary>
/// One file that left recon. Size is always recorded. Duration and rate are recorded
/// only when this job actually moved or deleted bytes (not Audit Mode, not Skip).
/// </summary>
public readonly record struct FileIoTransferObservation(
    FileIoBucket Bucket,
    long SizeBytes,
    DateTimeOffset At,
    double? DurationMs,
    double? RateBytesPerSec,
    string Outcome);

/// <summary>
/// Flattened descriptors from a <see cref="NumericSeries"/>. Empty bins have Count = 0
/// and null statistics — never NaN. <see cref="Series"/> is null when Count = 0
/// because Analytics rejects an empty snapshot.
/// </summary>
public sealed class FileIoSeriesSnapshot
{
    public required string Name { get; init; }
    public required string Unit { get; init; }
    public int Count { get; init; }
    public decimal? Min { get; init; }
    public decimal? Q1 { get; init; }
    public decimal? Median { get; init; }
    public decimal? Q3 { get; init; }
    public decimal? Max { get; init; }
    public double? Mean { get; init; }
    public decimal? P90 { get; init; }
    public decimal? P95 { get; init; }
    public decimal? P99 { get; init; }
    public double? StdDev { get; init; }
    public double? CoefficientOfVariation { get; init; }
    public double? Skewness { get; init; }
    public int HighOutlierCount { get; init; }
    public double? MeanCiLower { get; init; }
    public double? MeanCiUpper { get; init; }
    public NumericSeries? Series { get; init; }

    public static FileIoSeriesSnapshot Empty(string name, string unit) => new()
    {
        Name = name,
        Unit = unit,
        Count = 0,
    };

    public static FileIoSeriesSnapshot From(IEnumerable<double> values, string name, string unit)
    {
        var list = values.Where(double.IsFinite).Select(v => (decimal)v).ToArray();
        if (list.Length == 0)
            return Empty(name, unit);

        var series = NumericSeries.FromDecimal(list, name);
        var full = series.Full;
        var ci = series.Confidence(0.95);
        return new FileIoSeriesSnapshot
        {
            Name = name,
            Unit = unit,
            Count = full.Count,
            Min = full.Min,
            Q1 = full.Q1,
            Median = full.Median,
            Q3 = full.Q3,
            Max = full.Max,
            Mean = full.Mean,
            P90 = full.Percentile(0.90),
            P95 = full.Percentile(0.95),
            P99 = full.Percentile(0.99),
            StdDev = full.StdDev,
            CoefficientOfVariation = full.CoefficientOfVariation,
            Skewness = full.Skewness,
            HighOutlierCount = full.HighOutliers.Count,
            MeanCiLower = ci.Mean.IsDefined ? ci.Mean.Lower : null,
            MeanCiUpper = ci.Mean.IsDefined ? ci.Mean.Upper : null,
            Series = series,
        };
    }
}

public sealed class FileIoBucketAnalytics
{
    public required FileIoBucket Id { get; init; }
    public required string Name { get; init; }
    public required FileIoSeriesSnapshot FileSizes { get; init; }
    public required FileIoSeriesSnapshot TransferRates { get; init; }
    public required FileIoSeriesSnapshot Durations { get; init; }
}

/// <summary>
/// Analytics snapshot taken at job finalize. FileIo is the host: it accumulates
/// observations, then constructs NumericSeries. Charting stays in Charts.
/// </summary>
public sealed class FileIoJobStats
{
    public required FileIoSeriesSnapshot FileSizes { get; init; }
    public required FileIoSeriesSnapshot TransferRates { get; init; }
    public required FileIoSeriesSnapshot Durations { get; init; }
    public required IReadOnlyList<FileIoBucketAnalytics> Buckets { get; init; }
    public long JobElapsedMs { get; init; }
    public double JobRateBytesPerSec { get; init; }
    public int ObservationCount { get; init; }
    public int TransferredCount { get; init; }

    public static FileIoJobStats From(
        IReadOnlyList<FileIoTransferObservation> observations,
        TimeSpan elapsed,
        long jobBytes)
    {
        var ms = Math.Max(0, elapsed.TotalMilliseconds);
        var jobRate = ms > 0 ? jobBytes * 1000.0 / ms : 0;
        static IEnumerable<double> Sizes(IEnumerable<FileIoTransferObservation> rows)
            => rows.Where(r => r.Outcome != "Fail").Select(r => (double)r.SizeBytes);
        static IEnumerable<double> Rates(IEnumerable<FileIoTransferObservation> rows)
            => rows.Where(r => r.RateBytesPerSec is > 0).Select(r => r.RateBytesPerSec!.Value);
        static IEnumerable<double> Durations(IEnumerable<FileIoTransferObservation> rows)
            => rows.Where(r => r.DurationMs is > 0).Select(r => r.DurationMs!.Value);

        var names = new[] { "Tiny", "Small", "Medium", "Large", "Huge" };
        var buckets = new FileIoBucketAnalytics[5];
        for (var i = 0; i < 5; i++)
        {
            var id = (FileIoBucket)i;
            var name = names[i];
            var rows = observations.Where(o => o.Bucket == id).ToArray();
            buckets[i] = new FileIoBucketAnalytics
            {
                Id = id,
                Name = name,
                FileSizes = FileIoSeriesSnapshot.From(Sizes(rows), $"file-size-bytes.{name}", "bytes"),
                TransferRates = FileIoSeriesSnapshot.From(Rates(rows), $"transfer-rate-Bps.{name}", "Bps"),
                Durations = FileIoSeriesSnapshot.From(Durations(rows), $"duration-ms.{name}", "ms"),
            };
        }

        return new FileIoJobStats
        {
            FileSizes = FileIoSeriesSnapshot.From(Sizes(observations), "file-size-bytes", "bytes"),
            TransferRates = FileIoSeriesSnapshot.From(Rates(observations), "transfer-rate-Bps", "Bps"),
            Durations = FileIoSeriesSnapshot.From(Durations(observations), "duration-ms", "ms"),
            Buckets = buckets,
            JobElapsedMs = (long)Math.Round(ms),
            JobRateBytesPerSec = jobRate,
            ObservationCount = observations.Count,
            TransferredCount = observations.Count(o => o.RateBytesPerSec is > 0),
        };
    }

    public string FormatLine(string jobId)
    {
        string N(double? v) => v is { } d && double.IsFinite(d)
            ? (Math.Abs(d) >= 100 ? Math.Round(d).ToString("0", CultureInfo.InvariantCulture) : d.ToString("0.00", CultureInfo.InvariantCulture))
            : "—";
        string D(decimal? v) => v is { } d ? N((double)d) : "—";
        return
            $"Stats job={jobId} sizes n={FileSizes.Count} mean={N(FileSizes.Mean)} P50={D(FileSizes.Median)} P95={D(FileSizes.P95)} " +
            $"highOutliers={FileSizes.HighOutlierCount} rates n={TransferRates.Count} meanBps={N(TransferRates.Mean)} P95Bps={D(TransferRates.P95)} " +
            $"jobBps={N(JobRateBytesPerSec)} elapsedMs={JobElapsedMs}";
    }
}
