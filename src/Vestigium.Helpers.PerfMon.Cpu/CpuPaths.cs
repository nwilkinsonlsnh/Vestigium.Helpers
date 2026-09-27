namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>
/// Default short job paths. Processor Information first, Processor if that object is absent.
/// Queue is always System. Parking is PC01.004.
/// </summary>
internal static class CpuPaths
{
    public const string SystemObject = "System";
    public const string QueueLength = "Processor Queue Length";

    public static readonly string[] Utilization =
    [
        "% Processor Time",
        "% Privileged Time",
        "% User Time"
    ];

    public static string InstanceOrTotal(string? instance)
        => string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();

    public static string UtilizationObject(ICounterInventory? inventory = null)
        => CpuCounterCatalog.CategoryPresent(CpuObjects.ProcessorInformation, inventory)
            ? CpuObjects.ProcessorInformation
            : CpuObjects.Processor;

    public static IReadOnlyList<CounterPath> DefaultJob(string? instance = "_Total", ICounterInventory? inventory = null)
    {
        var inst = InstanceOrTotal(instance);
        var obj = UtilizationObject(inventory);
        var rows = new List<CounterPath>(Utilization.Length + 1);
        foreach (var counter in Utilization)
            rows.Add(new CounterPath(obj, counter, inst, CpuCounterCatalog.UnitOf(counter)));
        rows.Add(new CounterPath(SystemObject, QueueLength, instance: "", unit: "count"));
        return rows;
    }
}
