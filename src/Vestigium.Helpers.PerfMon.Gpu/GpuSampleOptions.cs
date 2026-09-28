namespace Vestigium.Helpers.PerfMon.Gpu;

/// <summary>Probe options. Shared SampleJob owns the clock. No invented _Total.</summary>
public sealed class GpuSampleOptions
{
    public string? Instance { get; init; }
    public bool IncludeAllInstances { get; init; } = true;
    public int InstanceCap { get; init; } = CounterSet.DefaultCap;
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan? Duration { get; init; }
    public int? Count { get; init; }
    public bool AllowBurst { get; init; }
    public TimeProvider Clock { get; init; } = TimeProvider.System;
    public ICounterSource? Source { get; init; }
    public ICounterInventory? Inventory { get; init; }
}
