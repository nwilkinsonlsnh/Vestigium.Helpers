namespace Vestigium.Helpers.PerfMon.Disk;

/// <summary>
/// PhysicalDisk short job. Never emits LogicalDisk. Disks are listed once at job start.
/// </summary>
internal static class DiskPaths
{
    public static readonly string[] PhysicalShort =
    [
        PhysicalDisk.DiskBytesPerSec,
        PhysicalDisk.AvgDiskSecRead,
        PhysicalDisk.AvgDiskSecWrite,
        PhysicalDisk.AvgDiskQueueLength,
        PhysicalDisk.CurrentDiskQueueLength,
        PhysicalDisk.PercentDiskTime
    ];

    public static string InstanceOrTotal(string? instance)
        => string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();

    public static string ObjectName => PhysicalDisk.Category;

    public static IReadOnlyList<string> Disks(ICounterInventory? inventory, int cap)
    {
        if (cap <= 0)
            return Array.Empty<string>();

        var take = cap > int.MaxValue - 1 ? cap : cap + 1;
        return DiskCounterCatalog
            .LiveInstances(DiskObjects.PhysicalDisk, take, inventory)
            .Where(name => !name.Equals("_Total", StringComparison.OrdinalIgnoreCase))
            .Take(cap)
            .ToArray();
    }

    public static IReadOnlyList<CounterPath> Physical(string? instance = "_Total")
        => For(new DiskSampleOptions { Instance = instance ?? "_Total" });

    public static IReadOnlyList<CounterPath> For(DiskSampleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var inst = InstanceOrTotal(options.Instance);
        var rows = new List<CounterPath>();
        AddInstance(rows, inst);

        if (options.IncludeDisks)
        {
            foreach (var disk in Disks(options.Inventory, options.InstanceCap))
            {
                if (disk.Equals(inst, StringComparison.OrdinalIgnoreCase))
                    continue;
                AddInstance(rows, disk);
            }
        }

        return rows;
    }

    private static void AddInstance(List<CounterPath> rows, string instance)
    {
        foreach (var name in PhysicalShort)
            rows.Add(new CounterPath(ObjectName, name, instance, DiskCounterCatalog.UnitOf(name)));
    }
}
