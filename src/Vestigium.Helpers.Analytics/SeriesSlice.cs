using Vestigium.Helpers;

namespace Vestigium.Helpers.Analytics;

public enum SliceKind
{
    Full = 0,
    Q1 = 1,
    Q2 = 2,
    Q3 = 3,
    Q4 = 4,
    Iqr = 5
}

/// <summary>
/// One band of a <see cref="NumericSeries"/> with the full descriptor set.
/// </summary>
public sealed class SeriesSlice
{
    private readonly DescriptiveStatistics _stats;

    internal SeriesSlice(SliceKind kind, IReadOnlyList<decimal> values)
    {
        Kind = kind;
        _stats = DescriptiveStatistics.Compute(values);
    }

    public SliceKind Kind { get; }
    public int Count => _stats.Count;
    public bool IsEmpty => _stats.Count == 0;
    public IReadOnlyList<decimal> Values => _stats.Values;
    public IReadOnlyList<decimal> Sorted => _stats.Sorted;

    public decimal? Min => _stats.Min;
    public decimal? Q1 => _stats.Q1;
    public decimal? Median => _stats.Median;
    public decimal? Q3 => _stats.Q3;
    public decimal? Max => _stats.Max;
    public decimal? Range => _stats.Range;
    public decimal? Iqr => _stats.Iqr;
    public decimal? Midrange => _stats.Midrange;
    public decimal? Midhinge => _stats.Midhinge;
    public decimal? Trimean => _stats.Trimean;
    public decimal? TukeyLowerFence => _stats.TukeyLowerFence;
    public decimal? TukeyUpperFence => _stats.TukeyUpperFence;
    public IReadOnlyList<decimal> LowOutliers => _stats.LowOutliers;
    public IReadOnlyList<decimal> HighOutliers => _stats.HighOutliers;
    public IReadOnlyList<decimal> Outliers => _stats.Outliers;

    public decimal? Sum => _stats.Sum;
    public double? Mean => _stats.Mean;
    public double? SumOfSquaredDeviations => _stats.SumOfSquaredDeviations;
    public double? Variance => _stats.Variance;
    public double? PopulationVariance => _stats.PopulationVariance;
    public double? StdDev => _stats.StdDev;
    public double? PopulationStdDev => _stats.PopulationStdDev;
    public double? StandardErrorOfMean => _stats.StandardErrorOfMean;
    public double? CoefficientOfVariation => _stats.CoefficientOfVariation;
    public double? MeanAbsoluteDeviation => _stats.MeanAbsoluteDeviation;
    public double? MedianAbsoluteDeviation => _stats.MedianAbsoluteDeviation;
    public double? Skewness => _stats.Skewness;
    public double? ExcessKurtosis => _stats.ExcessKurtosis;
    public double? Kurtosis => _stats.Kurtosis;
    public double? GeometricMean => _stats.GeometricMean;
    public double? HarmonicMean => _stats.HarmonicMean;

    public FrequencyTable Frequency => _stats.Frequency;

    public decimal Percentile(double p) => _stats.Percentile(p);

    public IReadOnlyDictionary<double, decimal> NamedPercentiles()
    {
        if (IsEmpty)
        {
            HelperLog.Reject("cannot compute percentiles of an empty slice");
            throw new InvalidOperationException("Cannot compute percentiles of an empty slice.");
        }

        return new Dictionary<double, decimal>
        {
            [0.01] = Percentile(0.01),
            [0.05] = Percentile(0.05),
            [0.10] = Percentile(0.10),
            [0.25] = Q1!.Value,
            [0.50] = Median!.Value,
            [0.75] = Q3!.Value,
            [0.90] = Percentile(0.90),
            [0.95] = Percentile(0.95),
            [0.99] = Percentile(0.99)
        };
    }

    public ConfidenceReport Confidence(double level = ConfidenceLevel.DefaultValue)
        => ConfidenceReport.For(_stats, ConfidenceLevel.Of(level), populationSize: null);

    public ConfidenceReport Confidence(double level, int populationSize)
        => ConfidenceReport.For(_stats, ConfidenceLevel.Of(level), populationSize);

    public ConfidenceInterval ProportionAbove(decimal threshold, double level = ConfidenceLevel.DefaultValue)
    {
        var k = Values.Count(v => v > threshold);
        return ConfidenceReport.Wilson(k, Count, ConfidenceLevel.Of(level));
    }

    public ConfidenceInterval ProportionAtLeast(decimal threshold, double level = ConfidenceLevel.DefaultValue)
    {
        var k = Values.Count(v => v >= threshold);
        return ConfidenceReport.Wilson(k, Count, ConfidenceLevel.Of(level));
    }

    /// <summary>
    /// Process-control fences for this band. Charts draws the result; it does not compute it.
    /// </summary>
    public ControlLimits ControlLimits(
        ControlLimitMethod method = ControlLimitMethod.MeanPlusKSigma,
        double k = 3,
        double? floor = null)
    {
        using var _ = HelperLog.Begin(
            HelperLog.AppIds.Analytics,
            HelperLog.Subcategories.Limits,
            "ControlLimits",
            $"band={Kind} method={method} k={k} n={Count}");
        return Analytics.ControlLimits.Compute(Values, Mean, StdDev, method, k, floor);
    }

    internal DescriptiveStatistics Statistics => _stats;
}
