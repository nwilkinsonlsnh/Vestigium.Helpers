using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Process capability against <see cref="SpecLimits"/>.
/// Pp/Ppk use sample s. Cp/Cpk use σ̂ = MR̄ / d2 on <see cref="SliceKind.Full"/> only.
/// </summary>
public sealed class ProcessCapability
{
    /// <summary>Specs used for this report, already scored against the band.</summary>
    public required SpecLimits Spec { get; init; }

    /// <summary>Sample mean of the band.</summary>
    public double? Mean { get; init; }

    /// <summary>Sample standard deviation of the band.</summary>
    public double? StdDev { get; init; }

    /// <summary>Average moving range of span 2 when this was Full and MR̄ &gt; 0.</summary>
    public double? MovingRangeBar { get; init; }

    /// <summary>MR̄ / d2. Null when Cp is not defined.</summary>
    public double? WithinSigma { get; init; }

    /// <summary>(USL − LSL) / (6s). Null when either spec is missing or s is not positive.</summary>
    public double? Pp { get; init; }

    /// <summary>(x̄ − LSL) / (3s). Null when LSL is missing or s is not positive.</summary>
    public double? Ppl { get; init; }

    /// <summary>(USL − x̄) / (3s). Null when USL is missing or s is not positive.</summary>
    public double? Ppu { get; init; }

    /// <summary>Min of the one-sided Pp values that exist.</summary>
    public double? Ppk { get; init; }

    /// <summary>(USL − LSL) / (6 σ̂_w). Null off Full, or when MR̄ is not positive, or either spec is missing.</summary>
    public double? Cp { get; init; }

    /// <summary>(x̄ − LSL) / (3 σ̂_w).</summary>
    public double? Cpl { get; init; }

    /// <summary>(USL − x̄) / (3 σ̂_w).</summary>
    public double? Cpu { get; init; }

    /// <summary>Min of the one-sided Cp values that exist.</summary>
    public double? Cpk { get; init; }

    internal static ProcessCapability Overall(SeriesSlice slice, SpecLimits spec)
    {
        ArgumentNullException.ThrowIfNull(slice);
        ArgumentNullException.ThrowIfNull(spec);

        AnalyticsLog.Debug(
            AnalyticsEvents.LimitsEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Limits,
            "enter capability",
            properties: AnalyticsLog.Props(
                ("via", "Capability"),
                ("band", slice.Kind.ToString()),
                ("n", slice.Count.ToString())));

        var scored = spec.Against(slice.Values);
        var mean = slice.Mean;
        var overall = SidesOf(mean, slice.StdDev is > 0 ? slice.StdDev : null, spec);

        double? mrBar = null;
        double? within = null;
        CapabilitySides inside = default;
        if (slice.Kind == SliceKind.Full)
        {
            mrBar = AverageMovingRange(slice.Values);
            if (mrBar is > 0)
            {
                within = mrBar.Value / ControlLimits.D2Span2;
                inside = SidesOf(mean, within, spec);
            }
        }

        var report = new ProcessCapability
        {
            Spec = scored,
            Mean = mean,
            StdDev = slice.StdDev,
            MovingRangeBar = mrBar,
            WithinSigma = within,
            Pp = overall.Two,
            Ppl = overall.Lower,
            Ppu = overall.Upper,
            Ppk = overall.Min,
            Cp = inside.Two,
            Cpl = inside.Lower,
            Cpu = inside.Upper,
            Cpk = inside.Min
        };

        AnalyticsLog.Information(
            AnalyticsEvents.LimitsComputed,
            VestigiumStatus.Success,
            AnalyticsCatalog.Subcategories.Limits,
            "capability computed",
            properties: AnalyticsLog.Props(
                ("via", "Capability"),
                ("ppk", overall.Min?.ToString("G6")),
                ("cpk", inside.Min?.ToString("G6")),
                ("outside", scored.OutsideCount.ToString())));

        return report;
    }

    private static CapabilitySides SidesOf(double? mean, double? sigma, SpecLimits spec)
    {
        if (sigma is not > 0 || mean is not { } xbar)
            return default;

        var lower = spec.Lower is { } lsl ? (xbar - lsl) / (3d * sigma) : null;
        var upper = spec.Upper is { } usl ? (usl - xbar) / (3d * sigma) : null;
        var two = spec.Lower is { } lo && spec.Upper is { } hi
            ? (hi - lo) / (6d * sigma)
            : null;
        return new CapabilitySides(two, lower, upper, MinDefined(lower, upper));
    }

    private static double? AverageMovingRange(IReadOnlyList<decimal> encounterOrder)
    {
        if (encounterOrder.Count < 2)
            return null;
        double sum = 0;
        for (var i = 1; i < encounterOrder.Count; i++)
            sum += (double)Math.Abs(encounterOrder[i] - encounterOrder[i - 1]);
        var bar = sum / (encounterOrder.Count - 1);
        return bar > 0 ? bar : null;
    }

    internal static double? MinDefined(double? a, double? b)
    {
        if (a is { } left && b is { } right)
            return Math.Min(left, right);
        return a ?? b;
    }

    private readonly record struct CapabilitySides(double? Two, double? Lower, double? Upper, double? Min);
}

/// <summary>Capability doors on a snapshot or band.</summary>
public static class SeriesCapability
{
    /// <summary>Pp/Ppk of Full, and Cp/Cpk when Full can form MR̄.</summary>
    public static ProcessCapability Capability(this NumericSeries series, SpecLimits spec)
    {
        ArgumentNullException.ThrowIfNull(series);
        return ProcessCapability.Overall(series.Full, spec);
    }

    /// <summary>Pp/Ppk of this band. Cp/Cpk only when <see cref="SliceKind.Full"/>.</summary>
    public static ProcessCapability Capability(this SeriesSlice slice, SpecLimits spec)
        => ProcessCapability.Overall(slice, spec);
}
