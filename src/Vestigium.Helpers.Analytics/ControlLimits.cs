using Vestigium.Helpers;
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
    /// Shewhart individuals using the average moving range of span 2.
    /// CL = mean, UCL/LCL = mean ± E2 × MR̄, E2 = 3 / d2, d2(n=2) = 1.1283791670955126.
    /// Moving ranges use encounter order, not the sorted copy.
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
    /// <summary>Unbiasing constant d2 for a moving range of two consecutive points.</summary>
    public const double D2Span2 = 1.1283791670955126;

    /// <summary>E2 = 3 / d2. Three-sigma individuals chart using MR̄.</summary>
    public const double E2Span2 = 3d / D2Span2;

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

    /// <summary>Locked baseline / SLA fences. Not computed from a sample.</summary>
    public static ControlLimits FromCaller(double center, double upper, double lower)
    {
        using var _ = HelperLog.Begin(
            HelperLog.AppIds.Analytics,
            HelperLog.Subcategories.Limits,
            "FromCaller",
            $"CL={center} UCL={upper} LCL={lower}");
        ValidateBand(center, upper, lower);
        var limits = new ControlLimits
        {
            Center = center,
            Upper = upper,
            Lower = lower,
            Method = ControlLimitMethod.CallerSupplied
        };
        HelperLog.Information(
            HelperLog.AppIds.Analytics,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Limits,
            $"caller-supplied CL={center} UCL={upper} LCL={lower}");
        return limits;
    }

    internal static ControlLimits Compute(
        IReadOnlyList<decimal> encounterOrder,
        double? mean,
        double? stdDev,
        ControlLimitMethod method,
        double k,
        double? floor)
    {
        if (method == ControlLimitMethod.CallerSupplied)
        {
            HelperLog.Reject("CallerSupplied limits must use ControlLimits.FromCaller");
            throw new ArgumentException("Caller-supplied limits must use ControlLimits.FromCaller.", nameof(method));
        }

        HelperGuard.NotNull(encounterOrder, nameof(encounterOrder));
        if (k <= 0)
        {
            HelperLog.Reject($"k={k} is not positive");
            throw new ArgumentOutOfRangeException(nameof(k), "k must be greater than 0.");
        }

        if (encounterOrder.Count < 2)
        {
            HelperLog.Reject("control limits require n>=2");
            throw new InvalidOperationException("Control limits require at least two observations.");
        }

        if (mean is null)
        {
            HelperLog.Reject("mean is undefined");
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
                HelperLog.Reject("stddev is undefined or zero");
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
                HelperLog.Reject("moving-range bar is zero");
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

        HelperLog.Information(
            HelperLog.AppIds.Analytics,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Limits,
            $"computed method={method} CL={center:G6} UCL={upper:G6} LCL={lower:G6} k={k} outside={outside.Count} clamped={clamped}");
        return limits;
    }

    private static void ValidateBand(double center, double upper, double lower)
    {
        if (upper <= center || center <= lower)
        {
            HelperLog.Reject($"malformed limits UCL={upper} CL={center} LCL={lower}");
            throw new ArgumentException("UCL must be greater than CL and CL must be greater than LCL.");
        }
    }
}
