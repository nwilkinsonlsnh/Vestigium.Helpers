namespace Vestigium.Helpers.PerfMon.Memory;

/// <summary>PDH category NUMA Node Memory. Generated from EventCatalog/pdh-categories.json.</summary>
public static class NUMANodeMemory
{
    public const string Category = "NUMA Node Memory";
    public const string AvailableMBytes = "Available MBytes";
    public const string FreeZeroPageListMBytes = "Free & Zero Page List MBytes";
    public const string StandbyListMBytes = "Standby List MBytes";
    public const string TotalMBytes = "Total MBytes";

    public static IReadOnlyList<string> Counters { get; } =
    [
        AvailableMBytes,
        FreeZeroPageListMBytes,
        StandbyListMBytes,
        TotalMBytes,
    ];
}
