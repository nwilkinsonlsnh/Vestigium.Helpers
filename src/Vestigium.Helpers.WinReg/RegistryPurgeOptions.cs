namespace Vestigium.Helpers.WinReg;

public sealed class RegistryPurgeOptions
{
    public int? KeepLastBatches { get; init; }
    public TimeSpan? OlderThan { get; init; }
    public bool UndoneOnly { get; init; }
    public string? BatchId { get; init; }
    public string? ArchivePath { get; init; }
    public bool DryRun { get; init; }
    public bool Confirm { get; init; }
}

public sealed class RegistryPurgeResult
{
    public RegistryWriteStatus Status { get; init; }
    public string Path { get; init; } = "";
    public int BatchesKept { get; init; }
    public int BatchesRemoved { get; init; }
    public int MutsKept { get; init; }
    public long BytesBefore { get; init; }
    public long BytesAfter { get; init; }
    public bool DryRun { get; init; }
    public string? Reason { get; init; }
}
