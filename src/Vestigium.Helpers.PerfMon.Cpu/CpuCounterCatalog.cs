namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>
/// Cpu vocabulary on the shared <see cref="CounterSet"/>.
/// Live checks and the watch live in Vestigium.Helpers.PerfMon.
/// </summary>
public static class CpuCounterCatalog
{
    public const int DefaultCap = CounterSet.DefaultCap;

    private static readonly CounterSet Set = new(
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
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
        },
        "Category must be Processor, Processor Information, or Processor Performance.",
        UnitOf);

    public static IReadOnlyList<string> Categories => Set.Categories;

    public static bool IsKnownCategory(string category) => Set.IsKnownCategory(category);

    public static bool IsKnownCounter(string category, string counter) => Set.IsKnownCounter(category, counter);

    public static IReadOnlyList<string> Counters(string category) => Set.KnownCounters(category);

    public static bool CategoryPresent(string category, ICounterInventory? inventory = null)
        => Set.CategoryPresent(category, inventory);

    public static bool HasCounter(string category, string counter, string instance = "_Total", ICounterInventory? inventory = null)
        => Set.HasCounter(category, counter, instance, inventory);

    public static bool HasInstance(string category, string instance, ICounterInventory? inventory = null)
        => Set.HasInstance(category, instance, inventory);

    public static IReadOnlyList<string> LiveCounters(string category, string instance = "_Total", int cap = DefaultCap, ICounterInventory? inventory = null)
        => Set.LiveCounters(category, instance, cap, inventory);

    public static IReadOnlyList<string> LiveInstances(string category, int cap = DefaultCap, ICounterInventory? inventory = null)
        => Set.LiveInstances(category, cap, inventory);

    public static CatalogSnapshot Snapshot(
        string category,
        string instance = "_Total",
        int cap = DefaultCap,
        ICounterInventory? inventory = null,
        TimeProvider? clock = null)
        => Set.Snapshot(category, instance, cap, inventory, clock);

    public static Task WatchAsync(
        CatalogWatchOptions options,
        Action<CatalogSnapshot> onSnapshot,
        CancellationToken cancellationToken = default)
        => Set.WatchAsync(options, onSnapshot, cancellationToken);

    public static IReadOnlyList<CounterPath> Paths(
        string category,
        string instance = "_Total",
        IEnumerable<string>? counters = null)
        => Set.Paths(category, instance, counters);

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
}
