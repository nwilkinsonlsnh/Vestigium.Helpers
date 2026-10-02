namespace Vestigium.Helpers.SystemInfo;

/// <summary>Physical and commit snapshot. Every field is bytes. No display strings.</summary>
public readonly record struct MemoryFacts(
    ulong TotalBytes,
    ulong AvailableBytes,
    ulong InUseBytes,
    ulong CommitPeakBytes,
    ulong PagedBytes,
    ulong NonpagedBytes);
