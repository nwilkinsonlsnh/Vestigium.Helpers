namespace Vestigium.Helpers.Processes;

/// <summary>Machine-wide snapshot. Phase 4 fills CPU, commit, physical, topology. GPU and paging lists wait for Phase 6.</summary>
public sealed class SystemCounters
{
    public DateTimeOffset Timestamp { get; init; }
    public TimeSpan? Interval { get; init; }
    public double? CpuPercent { get; init; }
    public double? SystemCommitPercent { get; init; }
    public double? PhysicalMemoryPercent { get; init; }
    public long? IoThroughputBytesPerSec { get; init; }
    public double? GpuUsagePercent { get; init; }
    public long? GpuDedicatedBytes { get; init; }
    public long? GpuSystemBytes { get; init; }
    public long? CommitCurrent { get; init; }
    public long? CommitLimit { get; init; }
    public long? CommitPeak { get; init; }
    public long? CommitChange { get; init; }
    public double? CommitPeakToLimit { get; init; }
    public double? CommitCurrentToLimit { get; init; }
    public long? CommitCurrentK => CommitCurrent is long value ? value / 1024 : null;
    public long? CommitLimitK => CommitLimit is long value ? value / 1024 : null;
    public long? CommitPeakK => CommitPeak is long value ? value / 1024 : null;
    public long? PhysicalTotal { get; init; }
    public long? PhysicalAvailable { get; init; }
    public long? PhysicalTotalK => PhysicalTotal is long value ? value / 1024 : null;
    public long? PhysicalAvailableK => PhysicalAvailable is long value ? value / 1024 : null;
    public int? ProcessCount { get; init; }
    public int? ThreadCount { get; init; }
    public int? HandleCount { get; init; }
    public int? Cores { get; init; }
    public int? Sockets { get; init; }
    public int? LogicalProcessors { get; init; }
}
