namespace Vestigium.Helpers.PerfMon.Gpu;

/// <summary>
/// Gpu vocabulary on the shared <see cref="CounterSet"/>.
/// Live lists the box. Known never stands in when the object is missing.
/// </summary>
public static class GpuCounterCatalog
{
    public const int DefaultCap = CounterSet.DefaultCap;

    private static readonly CounterSet Set = new(
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [GpuObjects.Engine] =
            [
                "Utilization Percentage",
                "Running Time"
            ],
            [GpuObjects.ProcessMemory] =
            [
                "Dedicated Usage",
                "Shared Usage",
                "Total Committed",
                "Local Usage",
                "Non Local Usage"
            ],
            [GpuObjects.AdapterMemory] =
            [
                "Dedicated Usage",
                "Shared Usage",
                "Total Committed"
            ],
            [GpuObjects.LocalAdapterMemory] =
            [
                "Local Usage"
            ],
            [GpuObjects.NonLocalAdapterMemory] =
            [
                "Non Local Usage"
            ]
        },
        "Category must be a GPU catalog object.",
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
        if (name.Contains("Percentage", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith('%'))
            return "%";
        if (name.Equals("Running Time", StringComparison.OrdinalIgnoreCase))
            return "100ns";
        if (name.Contains("Usage", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Committed", StringComparison.OrdinalIgnoreCase))
            return "B";
        return string.Empty;
    }
}
