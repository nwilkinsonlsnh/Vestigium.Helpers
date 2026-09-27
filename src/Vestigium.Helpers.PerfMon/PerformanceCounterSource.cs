using System.Diagnostics;

namespace Vestigium.Helpers.PerfMon;

/// <summary>
/// Local PDH adapter. Missing category or instance is Unavailable.
/// Unexpected PDH failures throw. Not a remote collector.
/// </summary>
internal sealed class PerformanceCounterSource : ICounterSource
{
    public SampleRecord Read(CounterPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        try
        {
            if (!CategoryExists(path.Category))
                return SampleRecord.Unavailable(path);

            if (path.Instance.Length > 0 && !InstanceExists(path.Category, path.Instance))
                return SampleRecord.Unavailable(path);

            using var counter = Open(path);
            var value = counter.NextValue();
            return SampleRecord.Ok(path, value);
        }
        catch (InvalidOperationException)
        {
            return SampleRecord.Unavailable(path);
        }
        catch (ArgumentException)
        {
            return SampleRecord.Unavailable(path);
        }
    }

    public IReadOnlyList<string> ListInstances(string category, int cap)
    {
        if (cap <= 0)
            return Array.Empty<string>();

        var name = category?.Trim() ?? string.Empty;
        if (name.Length == 0 || !CategoryExists(name))
            return Array.Empty<string>();

        try
        {
            var cat = new PerformanceCounterCategory(name);
            if (cat.CategoryType == PerformanceCounterCategoryType.SingleInstance)
                return Array.Empty<string>();

            var names = cat.GetInstanceNames();
            if (names.Length <= cap)
                return names;
            return names.Take(cap).ToArray();
        }
        catch (InvalidOperationException)
        {
            return Array.Empty<string>();
        }
        catch (ArgumentException)
        {
            return Array.Empty<string>();
        }
    }

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
