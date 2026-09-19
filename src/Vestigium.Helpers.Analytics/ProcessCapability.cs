using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Process capability against <see cref="SpecLimits"/>.
/// Pp/Ppk use sample s. Cp/Cpk stay null until the within-σ door (PR03.003).
/// </summary>
public sealed class ProcessCapability
{
    /// <summary>Specs used for this report, already scored against the band.</summary>
    public required SpecLimits Spec { get; init; }

    /// <summary>Sample mean of the band.</summary>
    public double? Mean { get; init; }

    /// <summary>Sample standard deviation of the band.</summary>
    public double? StdDev { get; init; }

    /// <summary>(USL − LSL) / (6s). Null when either spec is missing or s is not positive.</summary>
    public double? Pp { get; init; }

    /// <summary>(x̄ − LSL) / (3s). Null when LSL is missing or s is not positive.</summary>
    public double? Ppl { get; init; }

    /// <summary>(USL − x̄) / (3s). Null when USL is missing or s is not positive.</summary>
    public double? Ppu { get; init; }

    /// <summary>Min of the one-sided Pp values that exist.</summary>
    public double? Ppk { get; init; }

    /// <summary>Within capability. Null in PR03.002.</summary>
    public double? Cp { get; init; }

    /// <summary>Within lower. Null in PR03.002.</summary>
    public double? Cpl { get; init; }

    /// <summary>Within upper. Null in PR03.002.</summary>
    public double? Cpu { get; init; }

    /// <summary>Within min. Null in PR03.002.</summary>
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
            properties: AnalyticsLog.Props(("via", "Pp"), ("n", slice.Count.ToString())));

        var scored = spec.Against(slice.Values);
        var mean = slice.Mean;
        var s = slice.StdDev is > 0 ? slice.StdDev : null;

        double? ppl = null, ppu = null, pp = null, ppk = null;
        if (s is { } sigma && mean is { } xbar)
        {
            if (spec.Lower is { } lsl)
                ppl = (xbar - lsl) / (3d * sigma);
            if (spec.Upper is { } usl)
                ppu = (usl - xbar) / (3d * sigma);
            if (spec.Lower is { } lo && spec.Upper is { } hi)
                pp = (hi - lo) / (6d * sigma);
            ppk = MinDefined(ppl, ppu);
        }

        var report = new ProcessCapability
        {
            Spec = scored,
            Mean = mean,
            StdDev = slice.StdDev,
            Pp = pp,
            Ppl = ppl,
            Ppu = ppu,
            Ppk = ppk
        };

        AnalyticsLog.Information(
            AnalyticsEvents.LimitsComputed,
            VestigiumStatus.Success,
            AnalyticsCatalog.Subcategories.Limits,
            "capability computed",
            properties: AnalyticsLog.Props(
                ("via", "Pp"),
                ("ppk", ppk?.ToString("G6")),
                ("outside", scored.OutsideCount.ToString())));

        return report;
    }

    internal static double? MinDefined(double? a, double? b)
    {
        if (a is { } left && b is { } right)
            return Math.Min(left, right);
        return a ?? b;
    }
}

/// <summary>Capability doors on a snapshot or band.</summary>
public static class SeriesCapability
{
    /// <summary>Pp/Ppk of <see cref="NumericSeries.Full"/> against <paramref name="spec"/>.</summary>
    public static ProcessCapability Capability(this NumericSeries series, SpecLimits spec)
    {
        ArgumentNullException.ThrowIfNull(series);
        return ProcessCapability.Overall(series.Full, spec);
    }

    /// <summary>Pp/Ppk of this band against <paramref name="spec"/>.</summary>
    public static ProcessCapability Capability(this SeriesSlice slice, SpecLimits spec)
        => ProcessCapability.Overall(slice, spec);
}
