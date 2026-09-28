namespace Vestigium.Helpers.PerfMon.Memory;

/// <summary>PDH objects this probe is allowed to name.</summary>
public static class MemoryObjects
{
    public const string Cache = "Cache";
    public const string HyperVDynamicMemory = "Hyper-V Dynamic Memory Integration Service";
    public const string Memory = "Memory";
    public const string NumaNodeMemory = "NUMA Node Memory";
    public const string ReadyBoostCache = "ReadyBoost Cache";

    public static IReadOnlyList<string> All { get; } =
    [
        Cache,
        HyperVDynamicMemory,
        Memory,
        NumaNodeMemory,
        ReadyBoostCache
    ];
}
