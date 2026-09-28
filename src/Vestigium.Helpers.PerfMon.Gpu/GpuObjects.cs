namespace Vestigium.Helpers.PerfMon.Gpu;

/// <summary>PDH objects this probe is allowed to name.</summary>
public static class GpuObjects
{
    public const string Engine = "GPU Engine";
    public const string ProcessMemory = "GPU Process Memory";
    public const string AdapterMemory = "GPU Adapter Memory";
    public const string LocalAdapterMemory = "GPU Local Adapter Memory";
    public const string NonLocalAdapterMemory = "GPU Non Local Adapter Memory";

    public static IReadOnlyList<string> All { get; } =
    [
        Engine,
        ProcessMemory,
        AdapterMemory,
        LocalAdapterMemory,
        NonLocalAdapterMemory
    ];
}
