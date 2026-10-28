namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>Probe options. Shared SampleJob owns the clock.</summary>
public sealed class NetworkSampleOptions
{
    public string Instance { get; init; } = "_Total";
    public bool IncludeAdapters { get; init; }
    public int InstanceCap { get; init; } = CounterSet.DefaultCap;
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan? Duration { get; init; }
    public int? Count { get; init; }
    public bool AllowBurst { get; init; }
    public TimeProvider Clock { get; init; } = TimeProvider.System;
    public ICounterSource? Source { get; init; }
    public ICounterInventory? Inventory { get; init; }
}
