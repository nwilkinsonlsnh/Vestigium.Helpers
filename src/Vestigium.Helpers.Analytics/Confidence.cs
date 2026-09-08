using MathNet.Numerics.Distributions;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// A chosen coverage target. Frequentist confidence level is an input, never an estimate.
/// </summary>
public readonly record struct ConfidenceLevel
{
    public const double DefaultValue = 0.95;

    public static ConfidenceLevel Default { get; } = new(DefaultValue);

    public ConfidenceLevel(double value)
    {
        if (value is <= 0d or >= 1d)
        {
            HelperLog.Reject(
                HelperLog.AppIds.Analytics,
                HelperLog.Subcategories.Confidence,
                "ConfidenceLevel",
                $"γ={value} is not in (0, 1)");
            throw new ArgumentOutOfRangeException(nameof(value), "Confidence level must be in (0, 1).");
        }
        Value = value;
    }

    public double Value { get; }
    public double Alpha => 1d - Value;

    public static ConfidenceLevel Of(double value) => new(value);

    public static implicit operator double(ConfidenceLevel level) => level.Value;

    public static implicit operator ConfidenceLevel(double value) => new(value);

    public override string ToString() => Value.ToString("P2");
}

public readonly record struct ConfidenceInterval
{
    public required string Parameter { get; init; }
    public double? Estimate { get; init; }
    public double? Lower { get; init; }
    public double? Upper { get; init; }
    public double Level { get; init; }
    public required string Method { get; init; }
    public bool IsDefined { get; init; }

    public double? Width => Lower is { } lo && Upper is { } hi ? hi - lo : null;

    public static ConfidenceInterval Undefined(string parameter, double level, string method, double? estimate = null)
        => new()
        {
            Parameter = parameter,
            Estimate = estimate,
            Level = level,
            Method = method,
            IsDefined = false
        };

    public static ConfidenceInterval Defined(
        string parameter,
        double estimate,
        double lower,
        double upper,
        double level,
        string method)
        => new()
        {
            Parameter = parameter,
            Estimate = estimate,
            Lower = Math.Min(lower, upper),
            Upper = Math.Max(lower, upper),
            Level = level,
            Method = method,
            IsDefined = true
        };
}

public sealed class ConfidenceReport
{
    internal ConfidenceReport(
        ConfidenceLevel level,
        ConfidenceInterval mean,
        ConfidenceInterval median,
        ConfidenceInterval variance,
        ConfidenceInterval stdDev)
    {
        Level = level;
        Mean = mean;
        Median = median;
        Variance = variance;
        StdDev = stdDev;
    }

    public ConfidenceLevel Level { get; }
    public ConfidenceInterval Mean { get; }
    public ConfidenceInterval Median { get; }
    public ConfidenceInterval Variance { get; }
    public ConfidenceInterval StdDev { get; }

    internal static ConfidenceReport For(DescriptiveStatistics stats, ConfidenceLevel level, int? populationSize)
    {
        var mean = MeanInterval(stats, level, populationSize);
        var median = MedianInterval(stats, level);
        var variance = VarianceInterval(stats, level);
        var stdDev = variance.IsDefined
            ? ConfidenceInterval.Defined(
                "StdDev",
                stats.StdDev!.Value,
                Math.Sqrt(variance.Lower!.Value),
                Math.Sqrt(variance.Upper!.Value),
                level.Value,
                "Chi-square (sqrt of variance interval)")
            : ConfidenceInterval.Undefined("StdDev", level.Value, "Chi-square (sqrt of variance interval)", stats.StdDev);

        return new ConfidenceReport(level, mean, median, variance, stdDev);
    }

    internal static ConfidenceInterval MeanInterval(DescriptiveStatistics stats, ConfidenceLevel level, int? populationSize)
    {
        if (stats.Count < 2 || stats.Mean is null || stats.StdDev is null)
            return ConfidenceInterval.Undefined("Mean", level.Value, "Student t", stats.Mean);

        var n = stats.Count;
        if (populationSize is { } N)
        {
            if (N < 1)
            {
                HelperLog.Reject("populationSize must be at least 1");
                throw new ArgumentOutOfRangeException(nameof(populationSize), "Population size must be at least 1.");
            }
            if (n > N)
            {
                HelperLog.Reject($"n={n} exceeds N={N}");
                throw new ArgumentOutOfRangeException(nameof(populationSize), "Sample count cannot exceed population size.");
            }
        }

        var mean = stats.Mean.Value;
        var method = populationSize is null ? "Student t" : "Student t with finite-population correction";

        double se;
        if (populationSize is { } pop)
        {
            if (n == pop || pop == 1)
            {
                return ConfidenceInterval.Defined( "Mean", mean, mean, mean, level.Value, method);
            }

            var fpc = Math.Sqrt((pop - n) / (double)(pop - 1));
            se = stats.StdDev.Value / Math.Sqrt(n) * fpc;
        }
        else
        {
            se = stats.StandardErrorOfMean!.Value;
        }

        var df = n - 1;
        var t = StudentT.InvCDF(0d, 1d, df, 1d - level.Alpha / 2d);
        var half = t * se;
        return ConfidenceInterval.Defined("Mean", mean, mean - half, mean + half, level.Value, method);
    }

    internal static ConfidenceInterval MedianInterval(DescriptiveStatistics stats, ConfidenceLevel level)
    {
        if (stats.Count == 0 || stats.Median is null)
            return ConfidenceInterval.Undefined("Median", level.Value, "Order-statistic normal approximation");

        if (stats.Count == 1)
        {
            var v = (double)stats.Sorted[0];
            return ConfidenceInterval.Defined("Median", v, v, v, level.Value, "Order-statistic (n = 1)");
        }

        var z = Normal.InvCDF(0d, 1d, 1d - level.Alpha / 2d);
        var n = stats.Count;
        var root = Math.Sqrt(n);
        var j = (int)Math.Floor((n - z * root) / 2d);
        var k = (int)Math.Ceiling(1d + (n + z * root) / 2d);
        j = Math.Clamp(j, 1, n);
        k = Math.Clamp(k, 1, n);
        if (j > k)
            (j, k) = (k, j);

        return ConfidenceInterval.Defined(
            "Median",
            (double)stats.Median.Value,
            (double)stats.Sorted[j - 1],
            (double)stats.Sorted[k - 1],
            level.Value,
            "Order-statistic normal approximation");
    }

    internal static ConfidenceInterval VarianceInterval(DescriptiveStatistics stats, ConfidenceLevel level)
    {
        if (stats.Count < 2 || stats.Variance is null)
            return ConfidenceInterval.Undefined("Variance", level.Value, "Chi-square", stats.Variance);

        var df = stats.Count - 1;
        var ss = stats.Variance.Value * df;
        var loChi = ChiSquared.InvCDF(df, level.Alpha / 2d);
        var hiChi = ChiSquared.InvCDF(df, 1d - level.Alpha / 2d);
        if (loChi <= 0 || hiChi <= 0)
            return ConfidenceInterval.Undefined("Variance", level.Value, "Chi-square", stats.Variance);

        return ConfidenceInterval.Defined(
            "Variance",
            stats.Variance.Value,
            ss / hiChi,
            ss / loChi,
            level.Value,
            "Chi-square");
    }

    internal static ConfidenceInterval Wilson(int successes, int n, ConfidenceLevel level)
    {
        if (n <= 0)
            return ConfidenceInterval.Undefined("Proportion", level.Value, "Wilson score");

        var z = Normal.InvCDF(0d, 1d, 1d - level.Alpha / 2d);
        var z2 = z * z;
        var p = successes / (double)n;
        var denom = 1d + z2 / n;
        var center = (p + z2 / (2d * n)) / denom;
        var half = z * Math.Sqrt((p * (1d - p) + z2 / (4d * n)) / n) / denom;
        return ConfidenceInterval.Defined("Proportion", p, center - half, center + half, level.Value, "Wilson score");
    }

    internal static double? TwoSidedMeanPValue(DescriptiveStatistics stats, double hypothesizedMean)
    {
        if (stats.Count < 2 || stats.StandardErrorOfMean is not > 0 || stats.Mean is null)
            return null;

        var t = (stats.Mean.Value - hypothesizedMean) / stats.StandardErrorOfMean.Value;
        var df = stats.Count - 1;
        var cdf = StudentT.CDF(0d, 1d, df, t);
        var p = 2d * Math.Min(cdf, 1d - cdf);
        return Math.Clamp(p, 0d, 1d);
    }

    internal static int? PlanSampleSize(DescriptiveStatistics stats, double targetMargin, ConfidenceLevel level)
    {
        if (targetMargin <= 0)
        {
            HelperLog.Reject($"targetMargin={targetMargin} is not positive");
            throw new ArgumentOutOfRangeException(nameof(targetMargin), "Target margin must be positive.");
        }
        if (stats.StdDev is not > 0)
            return null;

        var s = stats.StdDev.Value;
        var n = Math.Max(2, stats.Count);
        for (var i = 0; i < 40; i++)
        {
            var t = StudentT.InvCDF(0d, 1d, n - 1, 1d - level.Alpha / 2d);
            var needed = (int)Math.Ceiling(Math.Pow(t * s / targetMargin, 2d));
            needed = Math.Max(2, needed);
            if (needed == n)
                return needed;
            n = needed;
        }

        return n;
    }
}
