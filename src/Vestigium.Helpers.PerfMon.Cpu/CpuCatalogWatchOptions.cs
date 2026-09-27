namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>Bounded watch over live counters and instances for one PDH object.</summary>
public sealed class CpuCatalogWatchOptions
{
    public string Category { get; init; } = CpuObjects.ProcessorInformation;
    public string Instance { get; init; } = "_Total";
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan? Duration { get; init; }
    public int? Count { get; init; }
    public bool AllowBurst { get; init; }
    public bool EmitOnlyOnChange { get; init; } = true;
    public int Cap { get; init; } = CpuCounterCatalog.DefaultCap;
    public TimeProvider Clock { get; init; } = TimeProvider.System;
    public ICpuInventory? Inventory { get; init; }
}
