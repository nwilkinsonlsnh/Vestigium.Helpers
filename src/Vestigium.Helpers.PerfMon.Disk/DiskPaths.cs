namespace Vestigium.Helpers.PerfMon.Disk;

/// <summary>
/// PhysicalDisk short job. Never emits LogicalDisk. Per-disk expansion is PD01.003.
/// </summary>
internal static class DiskPaths
{
    public static readonly string[] PhysicalShort =
    [
        "Disk Bytes/sec",
        "Avg. Disk sec/Read",
        "Avg. Disk sec/Write",
        "Avg. Disk Queue Length",
        "Current Disk Queue Length",
        "% Disk Time"
    ];

    public static string InstanceOrTotal(string? instance)
        => string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();

    public static IReadOnlyList<CounterPath> Physical(string? instance = "_Total")
        => For(new DiskSampleOptions { Instance = instance ?? "_Total" });

    public static IReadOnlyList<CounterPath> For(DiskSampleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var inst = InstanceOrTotal(options.Instance);
        return PhysicalShort
            .Select(name => new CounterPath(DiskObjects.PhysicalDisk, name, inst, DiskCounterCatalog.UnitOf(name)))
            .ToArray();
    }
}
