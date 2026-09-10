namespace Vestigium.Helpers.Processes;

/// <summary>
/// Immutable process snapshot. Phase 1 fills identity, image path, and resource counters.
/// Signer, command line, window, and mitigations wait for Phase 2.
/// </summary>
public sealed class ProcessInfo
{
    public int Pid { get; init; }
    public int? ParentPid { get; init; }
    public bool? ParentAlive { get; init; }
    public required string Name { get; init; }
    public int? SessionId { get; init; }
    public string? ImagePath { get; init; }
    public ProcessImageType ImageType { get; init; }
    public TimeSpan? CpuTime { get; init; }
    public double? CpuPercent { get; init; }
    public long? PrivateBytes { get; init; }
    public long? WorkingSet { get; init; }
    public long? IoReads { get; init; }
    public long? IoReadBytes { get; init; }
    public long? IoWrites { get; init; }
    public long? IoWriteBytes { get; init; }
    public double? GpuUsagePercent { get; init; }
    public long? GpuDedicatedBytes { get; init; }
    public long? GpuSystemBytes { get; init; }
    public IReadOnlyList<FieldAvailability> Availability { get; init; } = [];
}
