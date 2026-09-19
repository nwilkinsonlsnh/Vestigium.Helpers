using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>Which band of a <see cref="NumericSeries"/> a slice describes.</summary>
public enum SliceKind
{
    /// <summary>Every value in encounter order.</summary>
    Full = 0,
    /// <summary>Values ≤ the full-series Q1.</summary>
    Q1 = 1,
    /// <summary>Full-series Q1 < x ≤ median.</summary>
    Q2 = 2,
    /// <summary>Full-series median < x ≤ Q3.</summary>
    Q3 = 3,
    /// <summary>Values > the full-series Q3 (the slow group for latency).</summary>
    Q4 = 4,
    /// <summary>Full-series Q1 ≤ x ≤ Q3.</summary>
    Iqr = 5
}

/// <summary>
/// One band of a <see cref="NumericSeries"/> with the full descriptor set.
/// Empty bands return null descriptors and do not throw on property reads.
/// </summary>
public sealed class SeriesSlice
{
    private readonly DescriptiveStatistics _stats;

    internal SeriesSlice(SliceKind kind, IReadOnlyList<decimal> values)
    {
        Kind = kind;
        _stats = DescriptiveStatistics.Compute(values);
    }

    /// <summary>Which band this is.</summary>
    public SliceKind Kind { get; }
    /// <summary>Number of values in this band.</summary>
    public int Count => _stats.Count;
    /// <summary>True when the band has no values (typical for Q2–Q4 on a constant series).</summary>
    public bool IsEmpty => _stats.Count == 0;
    /// <summary>Frozen encounter-order values in this band.</summary>
    public IReadOnlyList<decimal> Values => _stats.Values;
    /// <summary>Frozen non-descending copy of <see cref="Values"/>.</summary>
    public IReadOnlyList<decimal> Sorted => _stats.Sorted;

    /// <summary>Smallest value, or null if empty.</summary>
    public decimal? Min => _stats.Min;
    /// <summary>PERCENTILE.INC 0.25 of this band.</summary>
    public decimal? Q1 => _stats.Q1;
    /// <summary>PERCENTILE.INC 0.50 of this band.</summary>
    public decimal? Median => _stats.Median;
    /// <summary>PERCENTILE.INC 0.75 of this band.</summary>
    public decimal? Q3 => _stats.Q3;
    /// <summary>Largest value, or null if empty.</summary>
    public decimal? Max => _stats.Max;
    /// <summary>Max − Min.</summary>
    public decimal? Range => _stats.Range;
    /// <summary>Q3 − Q1 of this band.</summary>
    public decimal? Iqr => _stats.Iqr;
    /// <summary>(Min + Max) / 2.</summary>
    public decimal? Midrange => _stats.Midrange;
    /// <summary>(Q1 + Q3) / 2.</summary>
    public decimal? Midhinge => _stats.Midhinge;
    /// <summary>(Q1 + 2 × Median + Q3) / 4.</summary>
    public decimal? Trimean => _stats.Trimean;
    /// <summary>Q1 − 1.5 × IQR.</summary>
    public decimal? TukeyLowerFence => _stats.TukeyLowerFence;
    /// <summary>Q3 + 1.5 × IQR.</summary>
    public decimal? TukeyUpperFence => _stats.TukeyUpperFence;
    /// <summary>Values below the lower Tukey fence, encounter order.</summary>
    public IReadOnlyList<decimal> LowOutliers => _stats.LowOutliers;
    /// <summary>Values above the upper Tukey fence, encounter order.</summary>
    public IReadOnlyList<decimal> HighOutliers => _stats.HighOutliers;
    /// <summary>Outliers in encounter order.</summary>
    public IReadOnlyList<decimal> Outliers => _stats.Outliers;
    /// <summary>Indexes into <see cref="Values"/> of low Tukey outliers.</summary>
    public IReadOnlyList<int> LowOutlierIndexes => _stats.LowOutlierIndexes;
    /// <summary>Indexes into <see cref="Values"/> of high Tukey outliers.</summary>
    public IReadOnlyList<int> HighOutlierIndexes => _stats.HighOutlierIndexes;
    /// <summary>Indexes into <see cref="Values"/> of every Tukey outlier, encounter order.</summary>
    public IReadOnlyList<int> OutlierIndexes => _stats.OutlierIndexes;

    /// <summary>Sum of this band.</summary>
    public decimal? Sum => _stats.Sum;
    /// <summary>Arithmetic mean.</summary>
    public double? Mean => _stats.Mean;
    /// <summary>Σ(x − mean)².</summary>
    public double? SumOfSquaredDeviations => _stats.SumOfSquaredDeviations;
    /// <summary>Sample variance SSD / (n − 1).</summary>
    public double? Variance => _stats.Variance;
    /// <summary>Population variance SSD / n.</summary>
    public double? PopulationVariance => _stats.PopulationVariance;
    /// <summary>Sample standard deviation.</summary>
    public double? StdDev => _stats.StdDev;
    /// <summary>Population standard deviation.</summary>
    public double? PopulationStdDev => _stats.PopulationStdDev;
    /// <summary>s / √n.</summary>
    public double? StandardErrorOfMean => _stats.StandardErrorOfMean;
    /// <summary>s / mean when mean ≠ 0.</summary>
    public double? CoefficientOfVariation => _stats.CoefficientOfVariation;
    /// <summary>Mean of |x − mean|.</summary>
    public double? MeanAbsoluteDeviation => _stats.MeanAbsoluteDeviation;
    /// <summary>Median of |x − median|.</summary>
    public double? MedianAbsoluteDeviation => _stats.MedianAbsoluteDeviation;
    /// <summary>Excel SKEW (G1). Null when n < 3 or s = 0.</summary>
    public double? Skewness => _stats.Skewness;
    /// <summary>Excel KURT (G2). Null when n < 4 or s = 0.</summary>
    public double? ExcessKurtosis => _stats.ExcessKurtosis;
    /// <summary>ExcessKurtosis + 3.</summary>
    public double? Kurtosis => _stats.Kurtosis;
    /// <summary>Defined only when every value is positive.</summary>
    public double? GeometricMean => _stats.GeometricMean;
    /// <summary>Defined only when every value is positive.</summary>
    public double? HarmonicMean => _stats.HarmonicMean;

    /// <summary>Exact multiplicities and Freedman–Diaconis histogram of this band.</summary>
    public FrequencyTable Frequency => _stats.Frequency;

    /// <summary>PERCENTILE.INC at p ∈ [0, 1]. Throws on an empty band.</summary>
    public decimal Percentile(double p) => _stats.Percentile(p);

    /// <summary>P01, P05, P10, Q1, median, Q3, P90, P95, P99. Throws on an empty band.</summary>
    public IReadOnlyDictionary<double, decimal> NamedPercentiles()
    {
        if (IsEmpty)
            Quantiles.RejectEmpty();

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

    /// <summary>Two-sided intervals at γ. Small n returns <see cref="ConfidenceInterval.IsDefined"/> false, not a throw.</summary>
    public ConfidenceReport Confidence(double level = ConfidenceLevel.DefaultValue)
        => ConfidenceReport.For(_stats, ConfidenceLevel.Of(level), populationSize: null);

    /// <summary>Mean interval with finite-population correction when N is known.</summary>
    public ConfidenceReport Confidence(double level, int populationSize)
        => ConfidenceReport.For(_stats, ConfidenceLevel.Of(level), populationSize);

    /// <summary>Wilson interval of the share of values strictly above <paramref name="threshold"/>.</summary>
    public ConfidenceInterval ProportionAbove(decimal threshold, double level = ConfidenceLevel.DefaultValue)
    {
        var successes = Values.Count(v => v > threshold);
        return ConfidenceReport.Wilson(successes, Count, ConfidenceLevel.Of(level));
    }

    /// <summary>Wilson interval of the share of values at least <paramref name="threshold"/>.</summary>
    public ConfidenceInterval ProportionAtLeast(decimal threshold, double level = ConfidenceLevel.DefaultValue)
    {
        var successes = Values.Count(v => v >= threshold);
        return ConfidenceReport.Wilson(successes, Count, ConfidenceLevel.Of(level));
    }

    /// <summary>
    /// Process fences for this band. Throws when the method cannot run.
    /// <see cref="ControlLimitMethod.MovingRange"/> is legal only on <see cref="SliceKind.Full"/>.
    /// </summary>
    public ControlLimits ControlLimits(
        ControlLimitMethod method = ControlLimitMethod.MeanPlusKSigma,
        double k = 3,
        double? floor = null)
    {
        AnalyticsLog.Debug(
            AnalyticsEvents.LimitsEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Limits,
            "enter limits",
            properties: AnalyticsLog.Props(("band", Kind.ToString()), ("method", method.ToString()), ("n", Count.ToString())));
        try
        {
            RejectMovingRangeOnBand(method, throwing: true);
            return Analytics.ControlLimits.Compute(Values, Mean, StdDev, method, k, floor);
        }
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.LimitsThrown, AnalyticsCatalog.Subcategories.Limits, ex);
            throw;
        }
    }

    /// <summary>
    /// Same math as <see cref="ControlLimits(ControlLimitMethod, double, double?)"/> but returns
    /// false when n is too small, s/MR is unusable, or MovingRange is asked of a value band.
    /// Still throws on caller errors (k ≤ 0, CallerSupplied).
    /// </summary>
    public bool TryControlLimits(
        out ControlLimits? limits,
        ControlLimitMethod method = ControlLimitMethod.MeanPlusKSigma,
        double k = 3,
        double? floor = null)
    {
        limits = null;
        AnalyticsLog.Debug(
            AnalyticsEvents.LimitsEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Limits,
            "enter limits",
            properties: AnalyticsLog.Props(
                ("band", Kind.ToString()),
                ("method", method.ToString()),
                ("n", Count.ToString()),
                ("via", "Try")));
        try
        {
            if (!RejectMovingRangeOnBand(method, throwing: false))
                return false;

            if (Analytics.ControlLimits.IsInsufficient(Values, Mean, StdDev, method, out var reason))
            {
                Analytics.ControlLimits.RejectLimits(reason, ("n", Count.ToString()), ("band", Kind.ToString()));
                return false;
            }

            limits = Analytics.ControlLimits.Compute(Values, Mean, StdDev, method, k, floor);
            return true;
        }
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.LimitsThrown, AnalyticsCatalog.Subcategories.Limits, ex);
            throw;
        }
    }

    private bool RejectMovingRangeOnBand(ControlLimitMethod method, bool throwing)
    {
        if (method != ControlLimitMethod.MovingRange || Kind == SliceKind.Full)
            return true;

        Analytics.ControlLimits.RejectLimits(
            "moving-range-band",
            ("band", Kind.ToString()));
        if (throwing)
            throw new ArgumentException(Analytics.ControlLimits.MovingRangeRequiresFull, nameof(method));
        return false;
    }

    internal DescriptiveStatistics Statistics => _stats;
}
