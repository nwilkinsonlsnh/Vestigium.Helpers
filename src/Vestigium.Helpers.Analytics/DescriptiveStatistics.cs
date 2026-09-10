namespace Vestigium.Helpers.Analytics;

internal sealed class DescriptiveStatistics
{
    public required int Count { get; init; }
    public IReadOnlyList<decimal> Values { get; init; } = [];
    public IReadOnlyList<decimal> Sorted { get; init; } = [];

    public decimal? Min { get; init; }
    public decimal? Q1 { get; init; }
    public decimal? Median { get; init; }
    public decimal? Q3 { get; init; }
    public decimal? Max { get; init; }
    public decimal? Range { get; init; }
    public decimal? Iqr { get; init; }
    public decimal? Midrange { get; init; }
    public decimal? Midhinge { get; init; }
    public decimal? Trimean { get; init; }
    public decimal? TukeyLowerFence { get; init; }
    public decimal? TukeyUpperFence { get; init; }
    public IReadOnlyList<decimal> LowOutliers { get; init; } = [];
    public IReadOnlyList<decimal> HighOutliers { get; init; } = [];
    public IReadOnlyList<decimal> Outliers { get; init; } = [];

    public decimal? Sum { get; init; }
    public double? Mean { get; init; }
    public double? SumOfSquaredDeviations { get; init; }
    public double? Variance { get; init; }
    public double? PopulationVariance { get; init; }
    public double? StdDev { get; init; }
    public double? PopulationStdDev { get; init; }
    public double? StandardErrorOfMean { get; init; }
    public double? CoefficientOfVariation { get; init; }
    public double? MeanAbsoluteDeviation { get; init; }
    public double? MedianAbsoluteDeviation { get; init; }
    public double? Skewness { get; init; }
    public double? ExcessKurtosis { get; init; }
    public double? Kurtosis { get; init; }
    public double? GeometricMean { get; init; }
    public double? HarmonicMean { get; init; }

    public required FrequencyTable Frequency { get; init; }

    public static DescriptiveStatistics Empty { get; } = new()
    {
        Count = 0,
        Frequency = FrequencyTable.Empty
    };

    public static DescriptiveStatistics Compute(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
            return Empty;

        var sorted = values.OrderBy(v => v).ToArray();
        var n = sorted.Length;
        var min = sorted[0];
        var max = sorted[^1];
        var q1 = Quantiles.Inclusive(sorted, 0.25);
        var median = Quantiles.Inclusive(sorted, 0.50);
        var q3 = Quantiles.Inclusive(sorted, 0.75);
        var iqr = q3 - q1;
        var range = max - min;
        var midrange = (min + max) / 2;
        var midhinge = (q1 + q3) / 2;
        var trimean = (q1 + 2 * median + q3) / 4;
        var lowerFence = q1 - 1.5m * iqr;
        var upperFence = q3 + 1.5m * iqr;
        var lowOutliers = values.Where(v => v < lowerFence).ToArray();
        var highOutliers = values.Where(v => v > upperFence).ToArray();
        var outliers = values.Where(v => v < lowerFence || v > upperFence).ToArray();

        decimal sum = 0;
        foreach (var v in values)
            sum += v;
        var mean = (double)sum / n;

        double ssd = 0;
        double mad = 0;
        var allPositive = true;
        double logSum = 0;
        decimal reciprocalSum = 0;
        foreach (var v in values)
        {
            var d = (double)v - mean;
            ssd += d * d;
            mad += Math.Abs(d);
            if (v <= 0)
                allPositive = false;
            else
            {
                logSum += Math.Log((double)v);
                reciprocalSum += 1m / v;
            }
        }

        var popVar = ssd / n;
        var sampleVar = n >= 2 ? ssd / (n - 1) : (double?)null;
        var popStd = Math.Sqrt(popVar);
        var sampleStd = sampleVar is { } sv ? Math.Sqrt(sv) : (double?)null;
        var sem = sampleStd is { } s ? s / Math.Sqrt(n) : (double?)null;
        var cv = sampleStd is { } s2 && mean != 0 ? s2 / mean : (double?)null;

        var absDevFromMedian = values.Select(v => Math.Abs(v - median)).OrderBy(v => v).ToArray();
        var medianAd = Quantiles.Inclusive(absDevFromMedian, 0.50);

        var skew = ComputeSkewness(values, mean, sampleStd);
        var g2 = ComputeExcessKurtosis(values, mean, sampleStd);

        return new DescriptiveStatistics
        {
            Count = n,
            Values = values,
            Sorted = sorted,
            Min = min,
            Q1 = q1,
            Median = median,
            Q3 = q3,
            Max = max,
            Range = range,
            Iqr = iqr,
            Midrange = midrange,
            Midhinge = midhinge,
            Trimean = trimean,
            TukeyLowerFence = lowerFence,
            TukeyUpperFence = upperFence,
            LowOutliers = lowOutliers,
            HighOutliers = highOutliers,
            Outliers = outliers,
            Sum = sum,
            Mean = mean,
            SumOfSquaredDeviations = ssd,
            Variance = sampleVar,
            PopulationVariance = popVar,
            StdDev = sampleStd,
            PopulationStdDev = popStd,
            StandardErrorOfMean = sem,
            CoefficientOfVariation = cv,
            MeanAbsoluteDeviation = mad / n,
            MedianAbsoluteDeviation = (double)medianAd,
            Skewness = skew,
            ExcessKurtosis = g2,
            Kurtosis = g2 is { } k ? k + 3 : null,
            GeometricMean = allPositive ? Math.Exp(logSum / n) : null,
            HarmonicMean = allPositive && reciprocalSum != 0 ? (double)(n / reciprocalSum) : null,
            Frequency = FrequencyTable.Build(values, iqr, min, max)
        };
    }

    public decimal Percentile(double p) =>
        Sorted.Count == 0
            ? throw new InvalidOperationException("Cannot compute a percentile of an empty slice.")
            : Quantiles.Inclusive(Sorted, p);

    internal static double? ComputeSkewness(IReadOnlyList<decimal> values, double mean, double? sampleStd)
    {
        var n = values.Count;
        if (n < 3 || sampleStd is not > 0)
            return null;
        var std = sampleStd.Value;
        double m3 = 0;
        foreach (var v in values)
        {
            var z = ((double)v - mean) / std;
            m3 += z * z * z;
        }

        return n * m3 / ((n - 1d) * (n - 2d));
    }

    internal static double? ComputeExcessKurtosis(IReadOnlyList<decimal> values, double mean, double? sampleStd)
    {
        var n = values.Count;
        if (n < 4 || sampleStd is not > 0)
            return null;
        var std = sampleStd.Value;
        double m4 = 0;
        foreach (var v in values)
        {
            var z = ((double)v - mean) / std;
            var z2 = z * z;
            m4 += z2 * z2;
        }

        var term1 = n * (n + 1d) * m4 / ((n - 1d) * (n - 2d) * (n - 3d));
        var term2 = 3d * (n - 1d) * (n - 1d) / ((n - 2d) * (n - 3d));
        return term1 - term2;
    }
}
