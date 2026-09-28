namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>
/// Network vocabulary on the shared <see cref="CounterSet"/>.
/// Most objects start with an empty known list; live lists the box.
/// </summary>
public static class NetworkCounterCatalog
{
    public const int DefaultCap = CounterSet.DefaultCap;

    private static readonly string[] InterfaceRates =
    [
        "Bytes Total/sec",
        "Bytes Received/sec",
        "Bytes Sent/sec",
        "Packets/sec",
        "Packets Received/sec",
        "Packets Sent/sec",
        "Packets Received Errors",
        "Packets Outbound Errors",
        "Packets Received Discarded",
        "Packets Outbound Discarded",
        "Output Queue Length",
        "Current Bandwidth"
    ];

    private static readonly CounterSet Set = new(BuildKnown(), "Category must be a Network catalog object.", UnitOf);

    public static IReadOnlyList<string> Categories => NetworkObjects.All;

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
        if (name.Contains("/sec", StringComparison.OrdinalIgnoreCase))
            return "/sec";
        if (name.Contains("Bandwidth", StringComparison.OrdinalIgnoreCase))
            return "bps";
        if (name.Contains("Queue", StringComparison.OrdinalIgnoreCase))
            return "count";
        if (name.Contains("Error", StringComparison.OrdinalIgnoreCase) || name.Contains("Discard", StringComparison.OrdinalIgnoreCase))
            return "count";
        return string.Empty;
    }

    private static Dictionary<string, string[]> BuildKnown()
    {
        var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in NetworkObjects.All)
            map[name] = [];
        map[NetworkObjects.NetworkInterface] = InterfaceRates;
        map[NetworkObjects.NetworkAdapter] = InterfaceRates;
        return map;
    }
}
