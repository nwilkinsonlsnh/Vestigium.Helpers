namespace Vestigium.Helpers.PerfMon.Disk;

/// <summary>
/// Disk vocabulary on the shared <see cref="CounterSet"/>.
/// Specialized objects may have an empty known list; live still lists the box.
/// </summary>
public static class DiskCounterCatalog
{
    public const int DefaultCap = CounterSet.DefaultCap;

    private static readonly string[] DiskRates =
    [
        "% Disk Time",
        "% Disk Read Time",
        "% Disk Write Time",
        "% Idle Time",
        "Disk Bytes/sec",
        "Disk Read Bytes/sec",
        "Disk Write Bytes/sec",
        "Disk Transfers/sec",
        "Disk Reads/sec",
        "Disk Writes/sec",
        "Avg. Disk sec/Transfer",
        "Avg. Disk sec/Read",
        "Avg. Disk sec/Write",
        "Avg. Disk Queue Length",
        "Avg. Disk Read Queue Length",
        "Avg. Disk Write Queue Length",
        "Current Disk Queue Length",
        "Avg. Disk Bytes/Transfer",
        "Avg. Disk Bytes/Read",
        "Avg. Disk Bytes/Write",
        "Split IO/Sec"
    ];

    private static readonly string[] LogicalExtra =
    [
        "% Free Space",
        "Free Megabytes"
    ];

    private static readonly CounterSet Set = new(
        BuildKnown(),
        "Category is not a Disk catalog object.",
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
        if (name.Contains("sec/", StringComparison.OrdinalIgnoreCase))
            return "s";
        if (name.Contains("/sec", StringComparison.OrdinalIgnoreCase) || name.Contains("/Sec", StringComparison.Ordinal))
            return "/sec";
        if (name.Contains("Megabytes", StringComparison.OrdinalIgnoreCase))
            return "MB";
        if (name.Contains("Queue", StringComparison.OrdinalIgnoreCase))
            return "count";
        return string.Empty;
    }

    private static Dictionary<string, string[]> BuildKnown()
    {
        var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in DiskObjects.All)
            map[name] = [];
        map[PhysicalDisk.Category] = [.. PhysicalDisk.Counters];
        map[LogicalDisk.Category] = [.. LogicalDisk.Counters];
        return map;
    }
}
