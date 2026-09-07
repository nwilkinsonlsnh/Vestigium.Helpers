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
}
