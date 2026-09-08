namespace Vestigium.Helpers.Analytics;

public sealed record FrequencyBin(decimal Value, int Count, double RelativeFrequency);

public sealed record HistogramBin(decimal LowerInclusive, decimal UpperInclusive, bool UpperIsClosed, int Count, double RelativeFrequency);

public sealed class FrequencyTable
{
    internal FrequencyTable(
        IReadOnlyList<FrequencyBin> frequencies,
        IReadOnlyList<decimal> modes,
        bool hasUniqueMode,
        double? entropyNats,
        IReadOnlyList<HistogramBin> histogram)
    {
        Frequencies = frequencies;
        Modes = modes;
        HasUniqueMode = hasUniqueMode;
        Mode = hasUniqueMode && modes.Count == 1 ? modes[0] : null;
        EntropyNats = entropyNats;
        Histogram = histogram;
        DistinctCount = frequencies.Count;
    }

    public static FrequencyTable Empty { get; } = new([], [], false, null, []);

    public IReadOnlyList<FrequencyBin> Frequencies { get; }
    public int DistinctCount { get; }
    public IReadOnlyList<decimal> Modes { get; }
    public decimal? Mode { get; }
    public bool HasUniqueMode { get; }
    public double? EntropyNats { get; }
    public IReadOnlyList<HistogramBin> Histogram { get; }

    internal static FrequencyTable Build(IReadOnlyList<decimal> values, decimal? iqr, decimal? min, decimal? max)
    {
        if (values.Count == 0)
            return Empty;

        var counts = new Dictionary<decimal, int>();
        foreach (var v in values)
        {
            counts.TryGetValue(v, out var n);
            counts[v] = n + 1;
        }

        var total = (double)values.Count;
        var frequencies = counts
            .Select(kv => new FrequencyBin(kv.Key, kv.Value, kv.Value / total))
            .OrderByDescending(b => b.Count)
            .ThenBy(b => b.Value)
            .ToArray();

        var maxCount = frequencies[0].Count;
        var modes = frequencies.Where(b => b.Count == maxCount).Select(b => b.Value).ToArray();
        var hasUniqueMode = maxCount >= 2 && modes.Length == 1;

        double entropy = 0;
        foreach (var bin in frequencies)
        {
            if (bin.RelativeFrequency <= 0)
                continue;
            entropy -= bin.RelativeFrequency * Math.Log(bin.RelativeFrequency);
        }

        var histogram = BuildHistogram(values, iqr, min!.Value, max!.Value);
        return new FrequencyTable(frequencies, modes, hasUniqueMode, entropy, histogram);
    }

    private static IReadOnlyList<HistogramBin> BuildHistogram(
        IReadOnlyList<decimal> values,
        decimal? iqr,
        decimal min,
        decimal max)
    {
        if (values.Count == 0)
            return [];

        if (min == max || iqr is null or 0)
        {
            return
            [
                new HistogramBin(min, max, UpperIsClosed: true, values.Count, 1d)
            ];
        }

        var width = 2d * (double)iqr.Value * Math.Pow(values.Count, -1d / 3d);
        if (width <= 0)
            width = (double)(max - min);

        var span = (double)(max - min);
        var binCount = Math.Max(1, (int)Math.Ceiling(span / width));
        binCount = Math.Min(binCount, Math.Max(1, values.Count));
        var step = (max - min) / binCount;
        if (step <= 0)
        {
            return
            [
                new HistogramBin(min, max, UpperIsClosed: true, values.Count, 1d)
            ];
        }

        var counts = new int[binCount];
        foreach (var v in values)
        {
            var idx = (int)decimal.Floor((v - min) / step);
            if (idx >= binCount)
                idx = binCount - 1;
            if (idx < 0)
                idx = 0;
            counts[idx]++;
        }

        var total = (double)values.Count;
        var bins = new HistogramBin[binCount];
        for (var i = 0; i < binCount; i++)
        {
            var lo = min + step * i;
            var hi = i == binCount - 1 ? max : min + step * (i + 1);
            bins[i] = new HistogramBin(lo, hi, UpperIsClosed: i == binCount - 1, counts[i], counts[i] / total);
        }

        return bins;
    }

    /// <summary>
    /// OLS line through histogram (midpoint, count) points. Same X as
    /// <see cref="Histogram"/>; Y is the fitted count. Callers draw this as
    /// the trend line — this type does not draw.
    /// </summary>
    public IReadOnlyList<ChartPoint> HistogramTrend()
    {
        var hist = Histogram;
        if (hist.Count == 0)
            return [];

        var n = hist.Count;
        double sumX = 0, sumY = 0, sumXy = 0, sumXx = 0;
        var mids = new double[n];
        for (var i = 0; i < n; i++)
        {
            var mid = (double)((hist[i].LowerInclusive + hist[i].UpperInclusive) / 2m);
            mids[i] = mid;
            var count = hist[i].Count;
            sumX += mid;
            sumY += count;
            sumXy += mid * count;
            sumXx += mid * mid;
        }

        var denom = n * sumXx - sumX * sumX;
        var slope = denom == 0 ? 0 : (n * sumXy - sumX * sumY) / denom;
        var intercept = (sumY - slope * sumX) / n;

        var points = new ChartPoint[n];
        for (var i = 0; i < n; i++)
            points[i] = new ChartPoint(mids[i], intercept + slope * mids[i]);
        return points;
    }

    /// <summary>
    /// Histogram bins sorted by count descending with a running share of n.
    /// That running share is the Pareto line (the 80/20 cumulative).
    /// </summary>
    public IReadOnlyList<ParetoPoint> Pareto()
    {
        var hist = Histogram;
        if (hist.Count == 0)
            return [];

        var ordered = hist
            .OrderByDescending(b => b.Count)
            .ThenBy(b => b.LowerInclusive)
            .ToArray();
        var total = ordered.Sum(b => b.Count);
        if (total <= 0)
            total = 1;

        var points = new ParetoPoint[ordered.Length];
        var cumulative = 0;
        for (var i = 0; i < ordered.Length; i++)
        {
            cumulative += ordered[i].Count;
            var mid = (double)((ordered[i].LowerInclusive + ordered[i].UpperInclusive) / 2m);
            points[i] = new ParetoPoint(i + 1, mid, ordered[i].Count, cumulative / (double)total);
        }

        return points;
    }
}
