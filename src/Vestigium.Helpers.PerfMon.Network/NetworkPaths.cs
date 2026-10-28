namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>
/// Network Interface short job. Adapters are listed once at job start.
/// </summary>
internal static class NetworkPaths
{
    public static string ObjectName => NetworkInterface.Category;

    public static readonly string[] InterfaceShort =
    [
        NetworkInterface.BytesTotalPerSec,
        NetworkInterface.BytesReceivedPerSec,
        NetworkInterface.BytesSentPerSec,
        NetworkInterface.PacketsPerSec,
        NetworkInterface.PacketsReceivedErrors,
        NetworkInterface.PacketsOutboundErrors,
        NetworkInterface.OutputQueueLength
    ];

    public static string InstanceOrTotal(string? instance)
        => string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();

    public static IReadOnlyList<string> Adapters(ICounterInventory? inventory, int cap)
    {
        if (cap <= 0)
            return Array.Empty<string>();

        var take = cap > int.MaxValue - 1 ? cap : cap + 1;
        return NetworkCounterCatalog
            .LiveInstances(ObjectName, take, inventory)
            .Where(name => !name.Equals("_Total", StringComparison.OrdinalIgnoreCase))
            .Take(cap)
            .ToArray();
    }

    public static IReadOnlyList<CounterPath> Interface(string? instance = "_Total")
        => For(new NetworkSampleOptions { Instance = instance ?? "_Total" });

    public static IReadOnlyList<CounterPath> For(NetworkSampleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var inst = InstanceOrTotal(options.Instance);
        var rows = new List<CounterPath>();
        AddInstance(rows, inst);

        if (options.IncludeAdapters)
        {
            foreach (var adapter in Adapters(options.Inventory, options.InstanceCap))
            {
                if (adapter.Equals(inst, StringComparison.OrdinalIgnoreCase))
                    continue;
                AddInstance(rows, adapter);
            }
        }

        return rows;
    }

    private static void AddInstance(List<CounterPath> rows, string instance)
    {
        foreach (var name in InterfaceShort)
            rows.Add(new CounterPath(ObjectName, name, instance, NetworkCounterCatalog.UnitOf(name)));
    }
}
