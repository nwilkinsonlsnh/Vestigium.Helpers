namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>
/// Default short job paths. Processor Information first, Processor if that object is absent.
/// Queue is always System. Parking is added only when that counter exists.
/// </summary>
internal static class CpuPaths
{
    public const string SystemObject = "System";
    public const string QueueLength = "Processor Queue Length";
    public const string ParkingStatus = "Parking Status";

    public static readonly string[] Utilization =
    [
        "% Processor Time",
        "% Privileged Time",
        "% User Time"
    ];

    public static string InstanceOrTotal(string? instance)
        => string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();

    public static string UtilizationObject(ICounterInventory? inventory = null)
    {
        if (CpuCounterCatalog.CategoryPresent(CpuObjects.ProcessorInformation, inventory))
            return CpuObjects.ProcessorInformation;
        CpuPerfLog.Information(
            CpuPerfEvents.CategoryFallback,
            Vestigium.Logging.VestigiumStatus.Success,
            CpuPerfCatalog.Subcategories.Paths,
            "Processor Information missing");
        return CpuObjects.Processor;
    }

    public static IReadOnlyList<string> Cores(ICounterInventory? inventory, int cap)
    {
        if (cap <= 0)
            return Array.Empty<string>();

        var take = cap > int.MaxValue - 1 ? cap : cap + 1;
        return CpuCounterCatalog
            .LiveInstances(UtilizationObject(inventory), take, inventory)
            .Where(name => !name.Equals("_Total", StringComparison.OrdinalIgnoreCase))
            .Take(cap)
            .ToArray();
    }

    public static IReadOnlyList<CounterPath> DefaultJob(string? instance = "_Total", ICounterInventory? inventory = null)
        => For(new CpuSampleOptions { Instance = instance ?? "_Total", Inventory = inventory }, inventory);

    public static IReadOnlyList<CounterPath> For(CpuSampleOptions options, ICounterInventory? inventory = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var inv = inventory ?? options.Inventory;
        var inst = InstanceOrTotal(options.Instance);
        var obj = UtilizationObject(inv);
        var rows = new List<CounterPath>();
        var park = options.IncludeParking
            && CpuCounterCatalog.HasCounter(CpuObjects.ProcessorInformation, ParkingStatus, inst, inv);
        if (options.IncludeParking && !park)
            CpuPerfLog.Debug(CpuPerfEvents.ParkingSkipped, Vestigium.Logging.VestigiumStatus.Success, CpuPerfCatalog.Subcategories.Paths, "parking counter absent");

        AddInstance(rows, obj, inst, park);

        if (options.IncludeCores)
        {
            foreach (var core in Cores(inv, options.InstanceCap))
            {
                if (core.Equals(inst, StringComparison.OrdinalIgnoreCase))
                    continue;
                AddInstance(rows, obj, core, park);
            }
        }

        rows.Add(new CounterPath(SystemObject, QueueLength, instance: "", unit: "count"));
        return rows;
    }

    private static void AddInstance(List<CounterPath> rows, string obj, string instance, bool park)
    {
        foreach (var counter in Utilization)
            rows.Add(new CounterPath(obj, counter, instance, CpuCounterCatalog.UnitOf(counter)));
        if (park)
            rows.Add(new CounterPath(CpuObjects.ProcessorInformation, ParkingStatus, instance, "flag"));
    }
}
