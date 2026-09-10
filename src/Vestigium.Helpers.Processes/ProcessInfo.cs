namespace Vestigium.Helpers.Processes;

/// <summary>Process snapshot. Identity fields are init-only; Phase 2 fields may be filled after construction.</summary>
public sealed class ProcessInfo
{
    public int Pid { get; init; }
    public int? ParentPid { get; init; }
    public bool? ParentAlive { get; init; }
    public required string Name { get; init; }
    public int? SessionId { get; init; }
    public string? ImagePath { get; init; }
    public ProcessImageType ImageType { get; set; }
    public string? Description { get; set; }
    public string? CompanyName { get; set; }
    public string? Version { get; set; }
    public SignerInfo? VerifiedSigner { get; set; }
    public string? PackageName { get; set; }
    public string? CommandLine { get; set; }
    public string? Comment { get; set; }
    public string? AutostartLocation { get; set; }
    public string? WindowTitle { get; set; }
    public WindowStatus WindowStatus { get; set; }
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
    public IntegrityLevel? IntegrityLevel { get; set; }
    public DepStatus? DepStatus { get; set; }
    public bool? AslrEnabled { get; set; }
    public MitigationState? ControlFlowGuard { get; set; }
    public MitigationState? StackProtection { get; set; }
    public bool? UiAccess { get; set; }
    public bool? Virtualized { get; set; }
    public ProcessProtection? Protection { get; set; }
    public DpiAwareness? DpiAwareness { get; set; }
    public string? EnterpriseContext { get; set; }
    public IReadOnlyList<FieldAvailability> Availability { get; set; } = [];
}
