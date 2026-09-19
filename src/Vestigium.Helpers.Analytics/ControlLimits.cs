using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// How <see cref="ControlLimits"/> were produced. A drawing surface consumes the numbers;
/// it does not pick a method.
/// </summary>
public enum ControlLimitMethod
{
    /// <summary>CL = mean, UCL/LCL = mean ± k × sample standard deviation.</summary>
    MeanPlusKSigma = 0,

    /// <summary>
    /// Shewhart individuals using the average moving range of span 2.
    /// CL = mean, UCL/LCL = mean ± E2 × MR̄, E2 = 3 / d2, d2(n=2) = 1.1283791670955126.
    /// Moving ranges use encounter order, not the sorted copy.
    /// Legal only on <see cref="SliceKind.Full"/>.
    /// </summary>
    MovingRange = 1,

    /// <summary>Caller supplied CL / UCL / LCL. Analytics does not compute them.</summary>
    CallerSupplied = 2
}

/// <summary>
/// Process-control fences for a snapshot. Formulae live here. A sibling drawing
/// library may consume this type; this project does not draw.
/// </summary>
public sealed class ControlLimits
{
    /// <summary>d2 for moving range of span 2.</summary>
    public const double D2Span2 = 1.1283791670955126;

    /// <summary>E2 = 3 / d2 for moving range of span 2.</summary>
    public const double E2Span2 = 3d / D2Span2;

    internal const string MovingRangeRequiresFull =
        "Moving-range limits require SliceKind.Full (encounter order of the process). Value bands are not a Shewhart individuals chart.";

    /// <summary>Center line (sample mean, or the caller-supplied center).</summary>
    public double Center { get; init; }

    /// <summary>Upper control limit.</summary>
    public double Upper { get; init; }

    /// <summary>Lower control limit, after any floor clamp.</summary>
    public double Lower { get; init; }

    /// <summary>
    /// k used for mean ± kσ. For moving-range this is 3 (the Shewhart convention inside E2).
    /// Null on caller-supplied fences.
    /// </summary>
    public double? K { get; init; }

    /// <summary>Which formula produced the fences.</summary>
    public ControlLimitMethod Method { get; init; }

    /// <summary>Average moving range of span 2, when <see cref="Method"/> is <see cref="ControlLimitMethod.MovingRange"/>.</summary>
    public double? MovingRangeBar { get; init; }

    /// <summary>E2 used with <see cref="MovingRangeBar"/>, when that method ran.</summary>
    public double? E2 { get; init; }

    /// <summary>Optional LCL floor supplied by the caller (typical 0 on a non-negative measure).</summary>
    public double? Floor { get; init; }

    /// <summary>Number of encounter-order values outside (Lower, Upper).</summary>
    public int OutOfControlCount { get; init; }

    /// <summary>Frozen encounter indexes of those values. Empty when none, never null.</summary>
    public IReadOnlyList<int> OutOfControlIndexes { get; init; } = [];

    /// <summary>
    /// Frozen per-step |xᵢ − xᵢ₋₁| in encounter order when moving-range ran.
    /// Empty for mean ± kσ and caller-supplied fences.
    /// </summary>
    public IReadOnlyList<double> MovingRanges { get; init; } = [];

    /// <summary>True when <paramref name="y"/> is strictly above <see cref="Upper"/> or below <see cref="Lower"/>.</summary>
    public bool IsOutOfControl(double y) => y > Upper || y < Lower;

    /// <summary>
    /// Caller-supplied band. Out-of-control indexes start empty; call <see cref="Against"/>
    /// to score a value list. Does not compute Center / Upper / Lower from a sample.
    /// </summary>
    /// <exception cref="ArgumentException">The band does not satisfy UCL &gt; CL &gt; LCL.</exception>
    public static ControlLimits FromCaller(double center, double upper, double lower)
    {
        AnalyticsLog.Debug(
            AnalyticsEvents.LimitsEnter,
            VestigiumStatus.Pending,
            AnalyticsCatalog.Subcategories.Limits,
            "enter limits",
            properties: AnalyticsLog.Props(("via", "FromCaller")));
        try
        {
            ValidateBand(center, upper, lower);
            var limits = new ControlLimits
            {
                Center = center,
                Upper = upper,
                Lower = lower,
                Method = ControlLimitMethod.CallerSupplied
            };
            AnalyticsLog.Information(
                AnalyticsEvents.LimitsCallerSupplied,
                VestigiumStatus.Success,
                AnalyticsCatalog.Subcategories.Limits,
                "caller-supplied limits",
                properties: AnalyticsLog.Props(
                    ("cl", center.ToString("G6")),
                    ("ucl", upper.ToString("G6")),
                    ("lcl", lower.ToString("G6"))));
            return limits;
        }
        catch (Exception ex)
        {
            AnalyticsLog.Unexpected(AnalyticsEvents.LimitsThrown, AnalyticsCatalog.Subcategories.Limits, ex);
            throw;
        }
    }

    /// <summary>
    /// Scores this finished band against encounter-order values.
    /// Copies the fences and recomputes <see cref="OutOfControlIndexes"/>.
    /// Does not recompute Center / Upper / Lower.
    /// </summary>
    /// <param name="encounterOrder">Values in process order. Null or empty yields count 0.</param>
    public ControlLimits Against(IReadOnlyList<decimal>? encounterOrder)
    {
        var values = encounterOrder ?? Array.Empty<decimal>();
        var outside = new List<int>();
        for (var i = 0; i < values.Count; i++)
        {
            var y = (double)values[i];
            if (y > Upper || y < Lower)
                outside.Add(i);
        }

        var scored = new ControlLimits
        {
            Center = Center,
            Upper = Upper,
            Lower = Lower,
            K = K,
            Method = Method,
            MovingRangeBar = MovingRangeBar,
            E2 = E2,
            Floor = Floor,
            OutOfControlCount = outside.Count,
            OutOfControlIndexes = NumberConvert.Freeze(outside),
            MovingRanges = NumberConvert.Freeze(MovingRanges)
        };

        if (outside.Count > 0)
        {
            AnalyticsLog.Warning(
                AnalyticsEvents.LimitsOutOfControl,
                VestigiumStatus.Warning,
                AnalyticsCatalog.Subcategories.Limits,
                "out-of-control points",
                properties: AnalyticsLog.Props(
                    ("via", "Against"),
                    ("method", Method.ToString()),
                    ("count", outside.Count.ToString()),
                    ("indexes", string.Join(",", outside))));
        }

        return scored;
    }

    internal static bool IsInsufficient(
        IReadOnlyList<decimal> encounterOrder,
        double? mean,
        double? stdDev,
        ControlLimitMethod method,
        out string reason)
    {
        if (encounterOrder.Count < 2)
        {
            reason = "n";
            return true;
        }

        if (mean is null)
        {
            reason = "mean";
            return true;
        }

        if (method == ControlLimitMethod.MeanPlusKSigma)
        {
            if (stdDev is null or 0)
            {
                reason = "stddev";
                return true;
            }

            reason = "";
            return false;
        }

        if (method == ControlLimitMethod.MovingRange)
        {
            double sum = 0;
            for (var i = 1; i < encounterOrder.Count; i++)
                sum += (double)Math.Abs(encounterOrder[i] - encounterOrder[i - 1]);
            if (sum / (encounterOrder.Count - 1) == 0)
            {
                reason = "mr";
                return true;
            }
        }

        reason = "";
        return false;
    }

    internal static void RejectLimits(string reason, params (string Key, string? Value)[] extra)
    {
        var pairs = new (string Key, string? Value)[extra.Length + 1];
        pairs[0] = ("reason", reason);
        extra.CopyTo(pairs, 1);
        AnalyticsLog.Error(
            AnalyticsEvents.LimitsRejected,
            VestigiumStatus.Failed,
            AnalyticsCatalog.Subcategories.Limits,
            "rejected limits",
            properties: AnalyticsLog.Props(pairs));
    }

    internal static ControlLimits Compute(
        IReadOnlyList<decimal> encounterOrder,
        double? mean,
        double? stdDev,
        ControlLimitMethod method,
        double k,
        double? floor)
    {
        ArgumentNullException.ThrowIfNull(encounterOrder);

        if (method == ControlLimitMethod.CallerSupplied)
        {
            RejectLimits("caller-supplied");
            throw new ArgumentException("Caller-supplied limits must use ControlLimits.FromCaller.", nameof(method));
        }

        if (k <= 0)
        {
            RejectLimits("k", ("k", k.ToString("G6")));
            throw new ArgumentOutOfRangeException(nameof(k), "k must be greater than 0.");
        }

        if (encounterOrder.Count < 2)
        {
            RejectLimits("n", ("n", encounterOrder.Count.ToString()));
            throw new InvalidOperationException("Control limits require at least two observations.");
        }

        if (mean is null)
        {
            RejectLimits("mean");
            throw new InvalidOperationException("Cannot compute control limits without a mean.");
        }

        double center = mean.Value;
        double upper;
        double lower;
        double? mrBar = null;
        double? e2 = null;
        IReadOnlyList<double> ranges = [];

        if (method == ControlLimitMethod.MeanPlusKSigma)
        {
            if (stdDev is null or 0)
            {
                RejectLimits("stddev");
                throw new InvalidOperationException("Mean ± kσ limits require a positive sample standard deviation.");
            }

            var width = k * stdDev.Value;
            upper = center + width;
            lower = center - width;
        }
        else
        {
            var mrs = new double[encounterOrder.Count - 1];
            double sum = 0;
            for (var i = 1; i < encounterOrder.Count; i++)
            {
                var mr = (double)Math.Abs(encounterOrder[i] - encounterOrder[i - 1]);
                mrs[i - 1] = mr;
                sum += mr;
            }

            mrBar = sum / mrs.Length;
            if (mrBar == 0)
            {
                RejectLimits("mr");
                throw new InvalidOperationException("Moving-range limits require a positive average moving range.");
            }

            ranges = mrs;
            e2 = E2Span2;
            var width = e2.Value * mrBar.Value;
            upper = center + width;
            lower = center - width;
        }

        var clamped = false;
        if (floor is { } floorValue)
        {
            if (lower < floorValue)
            {
                lower = floorValue;
                clamped = true;
            }
        }

        ValidateBand(center, upper, lower);

        var outside = new List<int>();
        for (var i = 0; i < encounterOrder.Count; i++)
        {
            var y = (double)encounterOrder[i];
            if (y > upper || y < lower)
                outside.Add(i);
        }

        var limits = new ControlLimits
        {
            Center = center,
            Upper = upper,
            Lower = lower,
            K = method == ControlLimitMethod.MeanPlusKSigma ? k : 3,
            Method = method,
            MovingRangeBar = mrBar,
            E2 = e2,
            Floor = floor,
            OutOfControlCount = outside.Count,
            OutOfControlIndexes = NumberConvert.Freeze(outside),
            MovingRanges = NumberConvert.Freeze(ranges)
        };

        AnalyticsLog.Information(
            AnalyticsEvents.LimitsComputed,
            VestigiumStatus.Success,
            AnalyticsCatalog.Subcategories.Limits,
            "limits computed",
            properties: AnalyticsLog.Props(
                ("method", method.ToString()),
                ("cl", center.ToString("G6")),
                ("ucl", upper.ToString("G6")),
                ("lcl", lower.ToString("G6")),
                ("k", k.ToString("G6")),
                ("outside", outside.Count.ToString()),
                ("clamped", clamped ? "true" : "false")));

        if (outside.Count > 0)
        {
            AnalyticsLog.Warning(
                AnalyticsEvents.LimitsOutOfControl,
                VestigiumStatus.Warning,
                AnalyticsCatalog.Subcategories.Limits,
                "out-of-control points",
                properties: AnalyticsLog.Props(
                    ("method", method.ToString()),
                    ("count", outside.Count.ToString()),
                    ("indexes", string.Join(",", outside))));
        }

        return limits;
    }

    private static void ValidateBand(double center, double upper, double lower)
    {
        if (upper <= center || center <= lower)
        {
            RejectLimits("band", ("ucl", upper.ToString("G6")), ("cl", center.ToString("G6")), ("lcl", lower.ToString("G6")));
            throw new ArgumentException("UCL must be greater than CL and CL must be greater than LCL.");
        }
    }
}
