using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// How <see cref="ControlLimits"/> were produced. Charts draws the numbers;
/// it does not pick a method.
/// </summary>
public enum ControlLimitMethod
{
    /// <summary>CL = mean, UCL/LCL = mean ± k × sample standard deviation.</summary>
    MeanPlusKSigma = 0,

    /// <summary>
    /// Some individuals using the average moving range of span 2.
    /// CL = mean, UCL/LCL = mean ± E2 × MR̄, E2 = 3 / d2, d2(n=2) = 1.1283791670955126.
    /// Moving ranges use encounter order, not the sorted copy.
    /// Legal only on <see cref="SliceKind.Full"/>.
    /// </summary>
    MovingRange = 1,

    /// <summary>Host supplied CL / UCL / LCL. Analytics does not compute them.</summary>
    CallerSupplied = 2
}

/// <summary>
/// Process-control fences for a snapshot. This type is the number contract
/// <c>Vestigium.Helpers.Charts</c> consumes. Formulae live here, not in Charts.
/// </summary>
public sealed class ControlLimits
{
    public const double D2Span2 = 1.1283791670955126;
    public const double E2Span2 = 3d / D2Span2;

    internal const string MovingRangeRequiresFull =
        "Moving-range limits require SliceKind.Full (encounter order of the process). Value bands are not a Shewhart individuals chart.";

    public double Center { get; init; }
    public double Upper { get; init; }
    public double Lower { get; init; }
    public double? K { get; init; }
    public ControlLimitMethod Method { get; init; }
    public double? MovingRangeBar { get; init; }
    public double? E2 { get; init; }
    public double? Floor { get; init; }
    public int OutOfControlCount { get; init; }
    public IReadOnlyList<int> OutOfControlIndexes { get; init; } = [];
    public IReadOnlyList<double> MovingRanges { get; init; } = [];

    public bool IsOutOfControl(double y) => y > Upper || y < Lower;

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
            OutOfControlIndexes = outside,
            MovingRanges = ranges
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
