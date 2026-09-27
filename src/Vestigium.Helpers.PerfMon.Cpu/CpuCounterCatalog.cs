using System.Diagnostics;

namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>
/// Counter names under Processor, Processor Information, and Processor Performance.
/// Known lists work without live PDH. Live lists read the box and stay capped.
/// </summary>
public static class CpuCounterCatalog
{
    public const int DefaultCap = 256;

    private static readonly Dictionary<string, string[]> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        [CpuObjects.Processor] =
        [
            "% Processor Time",
            "% User Time",
            "% Privileged Time",
            "% Interrupt Time",
            "% DPC Time",
            "% Idle Time",
            "Interrupts/sec",
            "DPCs Queued/sec",
            "DPC Rate",
            "C1 Transitions/sec",
            "C2 Transitions/sec",
            "C3 Transitions/sec",
            "% C1 Time",
            "% C2 Time",
            "% C3 Time"
        ],
        [CpuObjects.ProcessorInformation] =
        [
            "% Processor Time",
            "% User Time",
            "% Privileged Time",
            "% Interrupt Time",
            "% DPC Time",
            "% Idle Time",
            "% Priority Time",
            "Interrupts/sec",
            "DPCs Queued/sec",
            "DPC Rate",
            "Parking Status",
            "Processor Frequency",
            "% of Maximum Frequency",
            "Processor State Flags",
            "Idle Break Events/sec",
            "Clock Interrupts/sec",
            "Average Idle Time",
            "C1 Transitions/sec",
            "C2 Transitions/sec",
            "C3 Transitions/sec",
            "% C1 Time",
            "% C2 Time",
            "% C3 Time",
            "% Performance Limit",
            "Performance Limit Flags"
        ],
        [CpuObjects.ProcessorPerformance] =
        [
            "Processor Frequency",
            "% of Maximum Frequency",
            "% of Maximum Performance",
            "Processor Performance",
            "Processor Utility",
            "Privileged Utility",
            "% Performance Limit",
            "Performance Limit Flags",
            "Processor RTC"
        ]
    };

    public static IReadOnlyList<string> Categories => CpuObjects.All;

    public static bool IsKnownCategory(string category)
        => Known.ContainsKey(category?.Trim() ?? string.Empty);

    public static IReadOnlyList<string> Counters(string category)
    {
        var key = RequireCategory(category);
        return Known[key];
    }

    public static IReadOnlyList<string> LiveCounters(string category, string instance = "_Total", int cap = DefaultCap)
    {
        var key = RequireCategory(category);
        if (cap <= 0)
            return Array.Empty<string>();

        try
        {
            if (!PerformanceCounterCategory.Exists(key))
                return Counters(key);

            var inst = string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();
            var cat = new PerformanceCounterCategory(key);
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
            return Counters(key);
        }
        catch (ArgumentException)
        {
            return Counters(key);
        }
    }

    public static IReadOnlyList<string> LiveInstances(string category, int cap = DefaultCap)
    {
        var key = RequireCategory(category);
        if (cap <= 0)
            return Array.Empty<string>();

        try
        {
            if (!PerformanceCounterCategory.Exists(key))
                return Array.Empty<string>();

            var cat = new PerformanceCounterCategory(key);
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

    public static IReadOnlyList<CounterPath> Paths(
        string category,
        string instance = "_Total",
        IEnumerable<string>? counters = null)
    {
        var key = RequireCategory(category);
        var inst = string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();
        var names = counters is null ? Counters(key) : counters.Select(n => n?.Trim() ?? string.Empty).Where(n => n.Length > 0).ToArray();
        return names.Select(name => new CounterPath(key, name, inst, UnitOf(name))).ToArray();
    }

    public static string UnitOf(string counter)
    {
        var name = counter?.Trim() ?? string.Empty;
        if (name.StartsWith('%') || name.Contains("%", StringComparison.Ordinal))
            return "%";
        if (name.Contains("/sec", StringComparison.OrdinalIgnoreCase))
            return "/sec";
        if (name.Contains("Frequency", StringComparison.OrdinalIgnoreCase))
            return "MHz";
        if (name.Equals("Parking Status", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Flags", StringComparison.OrdinalIgnoreCase))
            return "flag";
        return string.Empty;
    }

    private static string RequireCategory(string? category)
    {
        var key = category?.Trim() ?? string.Empty;
        if (key.Length == 0 || !Known.ContainsKey(key))
            throw new ArgumentException(
                "Category must be Processor, Processor Information, or Processor Performance.",
                nameof(category));
        return Known.Keys.First(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
    }
}
