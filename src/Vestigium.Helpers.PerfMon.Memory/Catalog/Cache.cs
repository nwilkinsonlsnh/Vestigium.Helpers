namespace Vestigium.Helpers.PerfMon.Memory;

/// <summary>PDH category Cache. Generated from EventCatalog/pdh-categories.json.</summary>
public static class Cache
{
    public const string Category = "Cache";
    public const string AsyncCopyReadsPerSec = "Async Copy Reads/sec";
    public const string AsyncDataMapsPerSec = "Async Data Maps/sec";
    public const string AsyncFastReadsPerSec = "Async Fast Reads/sec";
    public const string AsyncMDLReadsPerSec = "Async MDL Reads/sec";
    public const string AsyncPinReadsPerSec = "Async Pin Reads/sec";
    public const string CopyReadHitsPercent = "Copy Read Hits %";
    public const string CopyReadsPerSec = "Copy Reads/sec";
    public const string DataFlushPagesPerSec = "Data Flush Pages/sec";
    public const string DataFlushesPerSec = "Data Flushes/sec";
    public const string DataMapHitsPercent = "Data Map Hits %";
    public const string DataMapPinsPerSec = "Data Map Pins/sec";
    public const string DataMapsPerSec = "Data Maps/sec";
    public const string DirtyPageThreshold = "Dirty Page Threshold";
    public const string DirtyPages = "Dirty Pages";
    public const string FastReadNotPossiblesPerSec = "Fast Read Not Possibles/sec";
    public const string FastReadResourceMissesPerSec = "Fast Read Resource Misses/sec";
    public const string FastReadsPerSec = "Fast Reads/sec";
    public const string LazyWriteFlushesPerSec = "Lazy Write Flushes/sec";
    public const string LazyWritePagesPerSec = "Lazy Write Pages/sec";
    public const string MDLReadHitsPercent = "MDL Read Hits %";
    public const string MDLReadsPerSec = "MDL Reads/sec";
    public const string PinReadHitsPercent = "Pin Read Hits %";
    public const string PinReadsPerSec = "Pin Reads/sec";
    public const string ReadAheadsPerSec = "Read Aheads/sec";
    public const string SyncCopyReadsPerSec = "Sync Copy Reads/sec";
    public const string SyncDataMapsPerSec = "Sync Data Maps/sec";
    public const string SyncFastReadsPerSec = "Sync Fast Reads/sec";
    public const string SyncMDLReadsPerSec = "Sync MDL Reads/sec";
    public const string SyncPinReadsPerSec = "Sync Pin Reads/sec";

    public static IReadOnlyList<string> Counters { get; } =
    [
        AsyncCopyReadsPerSec,
        AsyncDataMapsPerSec,
        AsyncFastReadsPerSec,
        AsyncMDLReadsPerSec,
        AsyncPinReadsPerSec,
        CopyReadHitsPercent,
        CopyReadsPerSec,
        DataFlushPagesPerSec,
        DataFlushesPerSec,
        DataMapHitsPercent,
        DataMapPinsPerSec,
        DataMapsPerSec,
        DirtyPageThreshold,
        DirtyPages,
        FastReadNotPossiblesPerSec,
        FastReadResourceMissesPerSec,
        FastReadsPerSec,
        LazyWriteFlushesPerSec,
        LazyWritePagesPerSec,
        MDLReadHitsPercent,
        MDLReadsPerSec,
        PinReadHitsPercent,
        PinReadsPerSec,
        ReadAheadsPerSec,
        SyncCopyReadsPerSec,
        SyncDataMapsPerSec,
        SyncFastReadsPerSec,
        SyncMDLReadsPerSec,
        SyncPinReadsPerSec,
    ];
}
