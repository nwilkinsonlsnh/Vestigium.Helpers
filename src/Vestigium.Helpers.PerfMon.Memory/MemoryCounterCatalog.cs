namespace Vestigium.Helpers.PerfMon.Memory;

/// <summary>
/// Memory vocabulary on the shared <see cref="CounterSet"/>.
/// Live lists the box. Known never stands in when the object is missing.
/// </summary>
public static class MemoryCounterCatalog
{
    public const int DefaultCap = CounterSet.DefaultCap;

    private static readonly CounterSet Set = new(
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [MemoryObjects.Memory] =
            [
                "Available MBytes",
                "Available Bytes",
                "Committed Bytes",
                "Commit Limit",
                "% Committed Bytes In Use",
                "Cache Bytes",
                "Pages/sec",
                "Page Faults/sec",
                "Pool Paged Bytes",
                "Pool Nonpaged Bytes"
            ],
            [MemoryObjects.Cache] =
            [
                "Copy Read Hits %",
                "Copy Reads/sec",
                "Data Map Hits %",
                "Lazy Write Flushes/sec",
                "Lazy Write Pages/sec"
            ],
            [MemoryObjects.NumaNodeMemory] =
            [
                "Available MBytes",
                "Free & Zero Page List MBytes"
            ],
            [MemoryObjects.ReadyBoostCache] = [],
            [MemoryObjects.HyperVDynamicMemory] = []
        },
        "Category must be a Memory catalog object.",
        UnitOf);

    public static IReadOnlyList<string> Categories => Set.Categories;

    public static bool IsKnownCategory(string category) => Set.IsKnownCategory(category);

    public static bool IsKnownCounter(string category, string counter) => Set.IsKnownCounter(category, counter);

    public static IReadOnlyList<string> Counters(string category) => Set.KnownCounters(category);

    public static bool CategoryPresent(string category, ICounterInventory? inventory = null)
        => Set.CategoryPresent(category, inventory);

    public static bool HasCounter(string category, string counter, string instance = "", ICounterInventory? inventory = null)
        => Set.HasCounter(category, counter, instance, inventory);

    public static bool HasInstance(string category, string instance, ICounterInventory? inventory = null)
        => Set.HasInstance(category, instance, inventory);

    public static IReadOnlyList<string> LiveCounters(string category, string instance = "", int cap = DefaultCap, ICounterInventory? inventory = null)
        => Set.LiveCounters(category, instance, cap, inventory);

    public static IReadOnlyList<string> LiveInstances(string category, int cap = DefaultCap, ICounterInventory? inventory = null)
        => Set.LiveInstances(category, cap, inventory);

    public static CatalogSnapshot Snapshot(
        string category,
        string instance = "",
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
        string instance = "",
        IEnumerable<string>? counters = null)
        => Set.Paths(category, instance, counters);

    public static string UnitOf(string counter)
    {
        var name = counter?.Trim() ?? string.Empty;
        if (name.StartsWith('%') || name.Contains("%", StringComparison.Ordinal))
            return "%";
        if (name.Contains("MBytes", StringComparison.OrdinalIgnoreCase))
            return "MB";
        if (name.Contains("/sec", StringComparison.OrdinalIgnoreCase))
            return "/sec";
        if (name.Contains("Bytes", StringComparison.OrdinalIgnoreCase))
            return "B";
        return string.Empty;
    }
}
