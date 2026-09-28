namespace Vestigium.Helpers.PerfMon.Memory;

/// <summary>PDH category ReadyBoost Cache. Generated from EventCatalog/pdh-categories.json.</summary>
public static class ReadyBoostCache
{
    public const string Category = "ReadyBoost Cache";
    public const string BytesCached = "Bytes cached";
    public const string CacheReadBytesPerSec = "Cache read bytes/sec";
    public const string CacheReadsPerSec = "Cache reads/sec";
    public const string CacheSpaceUsed = "Cache space used";
    public const string CompressionRatio = "Compression Ratio";
    public const string SkippedReadBytesPerSec = "Skipped read bytes/sec";
    public const string SkippedReadsPerSec = "Skipped reads/sec";
    public const string TotalCacheSizeBytes = "Total cache size bytes";
    public const string TotalReadBytesPerSec = "Total read bytes/sec";
    public const string TotalReadsPerSec = "Total reads/sec";

    public static IReadOnlyList<string> Counters { get; } =
    [
        BytesCached,
        CacheReadBytesPerSec,
        CacheReadsPerSec,
        CacheSpaceUsed,
        CompressionRatio,
        SkippedReadBytesPerSec,
        SkippedReadsPerSec,
        TotalCacheSizeBytes,
        TotalReadBytesPerSec,
        TotalReadsPerSec,
    ];
}
