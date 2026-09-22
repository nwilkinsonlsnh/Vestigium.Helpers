using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Caller-supplied specification limits. Not control limits.
/// Score a value list with <see cref="Against"/>.
/// </summary>
public sealed class SpecLimits
{
    /// <summary>Lower spec, if the caller supplied one.</summary>
    public double? Lower { get; init; }

    /// <summary>Upper spec, if the caller supplied one.</summary>
    public double? Upper { get; init; }

    /// <summary>Number of encounter-order values outside the specs.</summary>
    public int OutsideCount { get; init; }

    /// <summary>Frozen encounter indexes of those values. Empty when none, never null.</summary>
    public IReadOnlyList<int> OutsideIndexes { get; init; } = [];

    /// <summary>
    /// Builds a spec band. Indexes start empty; call <see cref="Against"/> to score.
    /// At least one side is required. When both are present, <paramref name="upper"/>
    /// must be greater than <paramref name="lower"/>.
    /// </summary>
    public static SpecLimits From(double? lower = null, double? upper = null)
    {
        AnalyticsLog.Debug(
            AnalyticsEvents.LimitsEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Limits,
            "enter spec",
            properties: AnalyticsLog.Props(("via", "SpecLimits.From")));
        try
        {
            Validate(lower, upper);
            return new SpecLimits { Lower = lower, Upper = upper };
        }
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.LimitsThrown, AnalyticsCatalog.Subcategories.Limits, ex);
            throw;
        }
    }

    /// <summary>True when <paramref name="y"/> is strictly below LSL or above USL.</summary>
    public bool IsOutside(double y)
    {
        if (Lower is { } lo && y < lo)
            return true;
        if (Upper is { } hi && y > hi)
            return true;
        return false;
    }

    /// <summary>
    /// Scores this spec against encounter-order values.
    /// Copies LSL/USL. Does not change the specs.
    /// Null or empty input → count 0, no throw.
    /// </summary>
    public SpecLimits Against(IReadOnlyList<decimal>? encounterOrder)
    {
        var values = encounterOrder ?? Array.Empty<decimal>();
        var outside = new List<int>();
        for (var i = 0; i < values.Count; i++)
        {
            if (IsOutside((double)values[i]))
                outside.Add(i);
        }

        var scored = new SpecLimits
        {
            Lower = Lower,
            Upper = Upper,
            OutsideCount = outside.Count,
            OutsideIndexes = NumberConvert.Freeze(outside)
        };

        if (outside.Count > 0)
        {
            AnalyticsLog.Warning(
                AnalyticsEvents.LimitsOutOfControl,
                VestigiumStatus.Warning,
                AnalyticsCatalog.Subcategories.Limits,
                "outside spec",
                properties: AnalyticsLog.Props(
                    ("via", "SpecLimits.Against"),
                    ("count", outside.Count.ToString()),
                    ("indexes", ControlLimits.FormatOutOfControlIndexes(outside))));
        }

        return scored;
    }

    private static void Validate(double? lower, double? upper)
    {
        if (lower is null && upper is null)
        {
            ControlLimits.RejectLimits("spec-missing");
            throw new ArgumentException("At least one specification limit is required.");
        }

        if (lower is { } lo && !double.IsFinite(lo))
        {
            ControlLimits.RejectLimits("spec-lsl");
            throw new ArgumentOutOfRangeException(nameof(lower), "LSL must be finite.");
        }

        if (upper is { } hi && !double.IsFinite(hi))
        {
            ControlLimits.RejectLimits("spec-usl");
            throw new ArgumentOutOfRangeException(nameof(upper), "USL must be finite.");
        }

        if (lower is { } a && upper is { } b && b <= a)
        {
            ControlLimits.RejectLimits("spec-band");
            throw new ArgumentException("USL must be greater than LSL when both are supplied.");
        }
    }
}
