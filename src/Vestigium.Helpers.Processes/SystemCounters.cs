namespace Vestigium.Helpers.Processes;

public sealed class GpuAdapterInfo
{
    public required string Name { get; init; }
    public double? UsagePercent { get; init; }
    public long? DedicatedBytes { get; init; }
    public long? SystemBytes { get; init; }
}

/// <summary>Machine-wide snapshot. Bytes unless a *K convenience property.</summary>
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
    public IReadOnlyList<GpuAdapterInfo> GpuAdapters { get; init; } = [];
    public long? ReadOperations { get; init; }
    public long? WriteOperations { get; init; }
    public long? OtherOperations { get; init; }
    public long? ReadBytes { get; init; }
    public long? WriteBytes { get; init; }
    public long? OtherBytes { get; init; }
    public long? ReadDelta { get; init; }
    public long? WriteDelta { get; init; }
    public long? OtherDelta { get; init; }
    public long? ReadBytesDelta { get; init; }
    public long? WriteBytesDelta { get; init; }
    public long? OtherBytesDelta { get; init; }
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
    public long? CacheWorkingSet { get; init; }
    public long? KernelWorkingSet { get; init; }
    public long? DriverWorkingSet { get; init; }
    public long? PhysicalTotalK => PhysicalTotal is long value ? value / 1024 : null;
    public long? PhysicalAvailableK => PhysicalAvailable is long value ? value / 1024 : null;
    public long? CacheWorkingSetK => CacheWorkingSet is long value ? value / 1024 : null;
    public long? PagedWorkingSet { get; init; }
    public long? PagedVirtual { get; init; }
    public long? PagedLimit { get; init; }
    public long? Nonpaged { get; init; }
    public long? NonpagedLimit { get; init; }
    public long? PageFaultDelta { get; init; }
    public long? PageReadDelta { get; init; }
    public long? PagingFileWriteDelta { get; init; }
    public long? MappedFileWriteDelta { get; init; }
    public long? Zeroed { get; init; }
    public long? Free { get; init; }
    public long? Modified { get; init; }
    public long? ModifiedNoWrite { get; init; }
    public long? Standby { get; init; }
    public long? Priority0 { get; init; }
    public long? Priority1 { get; init; }
    public long? Priority2 { get; init; }
    public long? Priority3 { get; init; }
    public long? Priority4 { get; init; }
    public long? Priority5 { get; init; }
    public long? Priority6 { get; init; }
    public long? Priority7 { get; init; }
    public long? PagedFileModified { get; init; }
    public int? ProcessCount { get; init; }
    public int? ThreadCount { get; init; }
    public int? HandleCount { get; init; }
    public long? ContextSwitchDelta { get; init; }
    public long? InterruptDelta { get; init; }
    public long? DpcDelta { get; init; }
    public int? Cores { get; init; }
    public int? Sockets { get; init; }
    public int? LogicalProcessors { get; init; }
}

internal sealed class SystemRawSnapshot
{
    public SystemTimes? Times { get; init; }
    public long ReadOperations { get; init; }
    public long WriteOperations { get; init; }
    public long OtherOperations { get; init; }
    public long ReadBytes { get; init; }
    public long WriteBytes { get; init; }
    public long OtherBytes { get; init; }
    public long PageFaults { get; init; }
    public long PageReads { get; init; }
    public long PagingFileWrites { get; init; }
    public long MappedFileWrites { get; init; }
    public long ContextSwitches { get; init; }
    public long Interrupts { get; init; }
    public long Dpcs { get; init; }
    public long CommitCurrent { get; init; }
}
