namespace Vestigium.Helpers.PerfMon.Gpu;

/// <summary>
/// Short job: GPU Engine utilization + GPU Adapter Memory dedicated.
/// Instances come from live lists. No invented _Total.
/// </summary>
internal static class GpuPaths
{
    public const string Utilization = "Utilization Percentage";
    public const string Dedicated = "Dedicated Usage";

    public static string InstanceOrNone(string? instance)
        => string.IsNullOrWhiteSpace(instance) ? string.Empty : instance.Trim();

    public static IReadOnlyList<string> Instances(string category, ICounterInventory? inventory, int cap)
    {
        if (cap <= 0)
            return Array.Empty<string>();

        return GpuCounterCatalog
            .LiveInstances(category, cap, inventory)
            .Where(name => name.Length > 0)
            .Take(cap)
            .ToArray();
    }

    public static IReadOnlyList<CounterPath> Short(string? instance = null)
        => For(new GpuSampleOptions { Instance = instance, IncludeAllInstances = false });

    public static IReadOnlyList<CounterPath> For(GpuSampleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var named = InstanceOrNone(options.Instance);
        var rows = new List<CounterPath>();

        foreach (var inst in Resolve(GpuObjects.Engine, named, options))
            rows.Add(new CounterPath(GpuObjects.Engine, Utilization, inst, GpuCounterCatalog.UnitOf(Utilization)));

        foreach (var inst in Resolve(GpuObjects.AdapterMemory, named, options))
            rows.Add(new CounterPath(GpuObjects.AdapterMemory, Dedicated, inst, GpuCounterCatalog.UnitOf(Dedicated)));

        return rows;
    }

    private static IReadOnlyList<string> Resolve(string category, string named, GpuSampleOptions options)
    {
        if (!options.IncludeAllInstances)
            return named.Length == 0 ? Array.Empty<string>() : [named];

        var live = Instances(category, options.Inventory, options.InstanceCap);
        if (named.Length == 0)
            return live;
        if (live.Contains(named, StringComparer.OrdinalIgnoreCase))
            return live;

        var rows = new List<string>(live.Count + 1) { named };
        rows.AddRange(live.Take(Math.Max(0, options.InstanceCap - 1)));
        return rows;
    }
}
