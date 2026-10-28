using System.Diagnostics;

namespace Vestigium.Helpers.PerfMon;

/// <summary>
/// Open-and-dispose PDH adapter. Rate counters stay at zero because each read is a new counter.
/// Not the public type. Public source is <see cref="CachedPdhSource"/>.
/// Missing category or instance is Unavailable. Unexpected PDH failures throw. Not a remote collector.
/// </summary>
internal sealed class PerformanceCounterSource : ICounterSource
{
    public SampleRecord Read(CounterPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        try
        {
            if (!CategoryExists(path.Category))
                return Miss(path);

            if (path.Instance.Length > 0 && !InstanceExists(path.Category, path.Instance))
                return Miss(path);

            using var counter = Open(path);
            var value = counter.NextValue();
            return SampleRecord.Ok(path, value);
        }
        catch (InvalidOperationException)
        {
            return Miss(path);
        }
        catch (ArgumentException)
        {
            return Miss(path);
        }
        catch (Exception ex)
        {
            PerfMonLog.Error(
                PerfMonEvents.SourceThrown,
                Vestigium.Logging.VestigiumStatus.Failed,
                PerfMonCatalog.Subcategories.Source,
                "unexpected PDH failure",
                ex);
            throw;
        }
    }

    private static SampleRecord Miss(CounterPath path)
    {
        PerfMonLog.Warning(
            PerfMonEvents.SourceUnavailable,
            Vestigium.Logging.VestigiumStatus.Failed,
            PerfMonCatalog.Subcategories.Source,
            "category or instance missing",
            properties: PerfMonLog.Props(("category", path.Category), ("counter", path.Counter), ("instance", path.Instance)));
        return SampleRecord.Unavailable(path);
    }

    public IReadOnlyList<string> ListInstances(string category, int cap)
        => PdhCounterInventory.Shared.LiveInstances(category, cap);

    public bool NeedsPrime(CounterPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        try
        {
            if (!CategoryExists(path.Category))
                return false;
            using var counter = Open(path);
            return IsRate(counter.CounterType);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool CategoryExists(string category)
    {
        try
        {
            return PerformanceCounterCategory.Exists(category);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool InstanceExists(string category, string instance)
    {
        try
        {
            return PerformanceCounterCategory.InstanceExists(instance, category);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static PerformanceCounter Open(CounterPath path)
        => path.Instance.Length == 0
            ? new PerformanceCounter(path.Category, path.Counter, readOnly: true)
            : new PerformanceCounter(path.Category, path.Counter, path.Instance, readOnly: true);

    private static bool IsRate(PerformanceCounterType type)
        => type is
            PerformanceCounterType.RateOfCountsPerSecond32 or
            PerformanceCounterType.RateOfCountsPerSecond64 or
            PerformanceCounterType.CountPerTimeInterval32 or
            PerformanceCounterType.CountPerTimeInterval64 or
            PerformanceCounterType.CounterTimer or
            PerformanceCounterType.CounterTimerInverse or
            PerformanceCounterType.Timer100Ns or
            PerformanceCounterType.Timer100NsInverse or
            PerformanceCounterType.ElapsedTime or
            PerformanceCounterType.SampleCounter or
            PerformanceCounterType.SampleFraction or
            PerformanceCounterType.CounterMultiTimer or
            PerformanceCounterType.CounterMultiTimerInverse or
            PerformanceCounterType.CounterMultiTimer100Ns or
            PerformanceCounterType.CounterMultiTimer100NsInverse or
            PerformanceCounterType.AverageTimer32 or
            PerformanceCounterType.RawFraction;
}
