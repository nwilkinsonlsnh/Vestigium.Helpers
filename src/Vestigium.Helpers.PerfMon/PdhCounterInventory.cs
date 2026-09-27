using System.Diagnostics;

namespace Vestigium.Helpers.PerfMon;

/// <summary>Local PDH inventory. Missing object is absent, not a known-list stand-in.</summary>
public sealed class PdhCounterInventory : ICounterInventory
{
    public static PdhCounterInventory Shared { get; } = new();

    public bool CategoryPresent(string category)
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

    public bool InstancePresent(string category, string instance)
    {
        if (!CategoryPresent(category))
            return false;
        if (string.IsNullOrWhiteSpace(instance))
            return true;

        try
        {
            var cat = new PerformanceCounterCategory(category);
            if (cat.CategoryType == PerformanceCounterCategoryType.SingleInstance)
                return true;
            return PerformanceCounterCategory.InstanceExists(instance.Trim(), category);
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

    public IReadOnlyList<string> LiveCounters(string category, string instance, int cap)
    {
        if (cap <= 0 || !CategoryPresent(category))
            return Array.Empty<string>();

        try
        {
            var cat = new PerformanceCounterCategory(category);
            var inst = string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();
            var rows = cat.CategoryType == PerformanceCounterCategoryType.SingleInstance
                ? cat.GetCounters()
                : cat.GetCounters(inst);

            return rows
                .Select(c => c.CounterName)
                .Where(n => n.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(cap)
                .ToArray();
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

    public IReadOnlyList<string> LiveInstances(string category, int cap)
    {
        if (cap <= 0 || !CategoryPresent(category))
            return Array.Empty<string>();

        try
        {
            var cat = new PerformanceCounterCategory(category);
            if (cat.CategoryType == PerformanceCounterCategoryType.SingleInstance)
                return Array.Empty<string>();

            var names = cat.GetInstanceNames();
            return names.Length <= cap ? names : names.Take(cap).ToArray();
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
}
