namespace Vestigium.Helpers.PerfMon.Memory;

/// <summary>PDH category Memory. Generated from EventCatalog/pdh-categories.json.</summary>
public static class Memory
{
    public const string Category = "Memory";
    public const string PercentCommittedBytesInUse = "% Committed Bytes In Use";
    public const string AvailableBytes = "Available Bytes";
    public const string AvailableKBytes = "Available KBytes";
    public const string AvailableMBytes = "Available MBytes";
    public const string CacheBytes = "Cache Bytes";
    public const string CacheBytesPeak = "Cache Bytes Peak";
    public const string CacheFaultsPerSec = "Cache Faults/sec";
    public const string CommitLimit = "Commit Limit";
    public const string CommittedBytes = "Committed Bytes";
    public const string DemandZeroFaultsPerSec = "Demand Zero Faults/sec";
    public const string FreeZeroPageListBytes = "Free & Zero Page List Bytes";
    public const string FreeSystemPageTableEntries = "Free System Page Table Entries";
    public const string LongTermAverageStandbyCacheLifetimeS = "Long-Term Average Standby Cache Lifetime (s)";
    public const string ModifiedPageListBytes = "Modified Page List Bytes";
    public const string PageFaultsPerSec = "Page Faults/sec";
    public const string PageReadsPerSec = "Page Reads/sec";
    public const string PageWritesPerSec = "Page Writes/sec";
    public const string PagesInputPerSec = "Pages Input/sec";
    public const string PagesOutputPerSec = "Pages Output/sec";
    public const string PagesPerSec = "Pages/sec";
    public const string PoolNonpagedAllocs = "Pool Nonpaged Allocs";
    public const string PoolNonpagedBytes = "Pool Nonpaged Bytes";
    public const string PoolPagedAllocs = "Pool Paged Allocs";
    public const string PoolPagedBytes = "Pool Paged Bytes";
    public const string PoolPagedResidentBytes = "Pool Paged Resident Bytes";
    public const string StandbyCacheCoreBytes = "Standby Cache Core Bytes";
    public const string StandbyCacheNormalPriorityBytes = "Standby Cache Normal Priority Bytes";
    public const string StandbyCacheReserveBytes = "Standby Cache Reserve Bytes";
    public const string SystemCacheResidentBytes = "System Cache Resident Bytes";
    public const string SystemCodeResidentBytes = "System Code Resident Bytes";
    public const string SystemCodeTotalBytes = "System Code Total Bytes";
    public const string SystemDriverResidentBytes = "System Driver Resident Bytes";
    public const string SystemDriverTotalBytes = "System Driver Total Bytes";
    public const string TransitionFaultsPerSec = "Transition Faults/sec";
    public const string TransitionPagesRePurposedPerSec = "Transition Pages RePurposed/sec";
    public const string WriteCopiesPerSec = "Write Copies/sec";

    public static IReadOnlyList<string> Counters { get; } =
    [
        PercentCommittedBytesInUse,
        AvailableBytes,
        AvailableKBytes,
        AvailableMBytes,
        CacheBytes,
        CacheBytesPeak,
        CacheFaultsPerSec,
        CommitLimit,
        CommittedBytes,
        DemandZeroFaultsPerSec,
        FreeZeroPageListBytes,
        FreeSystemPageTableEntries,
        LongTermAverageStandbyCacheLifetimeS,
        ModifiedPageListBytes,
        PageFaultsPerSec,
        PageReadsPerSec,
        PageWritesPerSec,
        PagesInputPerSec,
        PagesOutputPerSec,
        PagesPerSec,
        PoolNonpagedAllocs,
        PoolNonpagedBytes,
        PoolPagedAllocs,
        PoolPagedBytes,
        PoolPagedResidentBytes,
        StandbyCacheCoreBytes,
        StandbyCacheNormalPriorityBytes,
        StandbyCacheReserveBytes,
        SystemCacheResidentBytes,
        SystemCodeResidentBytes,
        SystemCodeTotalBytes,
        SystemDriverResidentBytes,
        SystemDriverTotalBytes,
        TransitionFaultsPerSec,
        TransitionPagesRePurposedPerSec,
        WriteCopiesPerSec,
    ];
}
