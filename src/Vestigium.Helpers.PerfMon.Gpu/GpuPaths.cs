namespace Vestigium.Helpers.PerfMon.Gpu;

/// <summary>
/// Short job: GPU Engine utilization + GPU Adapter Memory dedicated.
/// Does not invent _Total. Live expansion is PG01.003.
/// </summary>
internal static class GpuPaths
{
    public const string Utilization = "Utilization Percentage";
    public const string Dedicated = "Dedicated Usage";

    public static string InstanceOrNone(string? instance)
        => string.IsNullOrWhiteSpace(instance) ? string.Empty : instance.Trim();

    public static IReadOnlyList<CounterPath> Short(string? instance = null)
        => For(new GpuSampleOptions { Instance = instance, IncludeAllInstances = false });

    public static IReadOnlyList<CounterPath> For(GpuSampleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var inst = InstanceOrNone(options.Instance);
        return
        [
            new CounterPath(GpuObjects.Engine, Utilization, inst, GpuCounterCatalog.UnitOf(Utilization)),
            new CounterPath(GpuObjects.AdapterMemory, Dedicated, inst, GpuCounterCatalog.UnitOf(Dedicated))
        ];
    }
}
