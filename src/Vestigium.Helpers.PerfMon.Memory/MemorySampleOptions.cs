namespace Vestigium.Helpers.PerfMon.Memory;

/// <summary>Probe options. Shared SampleJob owns the clock. Empty instance stays empty.</summary>
public sealed class MemorySampleOptions
{
    public string Instance { get; init; } = "";
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan? Duration { get; init; }
    public int? Count { get; init; }
    public bool AllowBurst { get; init; }
    public TimeProvider Clock { get; init; } = TimeProvider.System;
    public ICounterSource? Source { get; init; }
    public ICounterInventory? Inventory { get; init; }
}
