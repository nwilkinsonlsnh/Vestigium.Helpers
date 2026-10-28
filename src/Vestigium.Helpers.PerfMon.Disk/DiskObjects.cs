namespace Vestigium.Helpers.PerfMon.Disk;

/// <summary>PDH objects this probe is allowed to name.</summary>
public static class DiskObjects
{
    public const string FileSystemDiskActivity = "FileSystem Disk Activity";
    public const string LogicalDisk = "LogicalDisk";
    public const string NtfsBucketizedPerformance = "Ntfs Bucketized Performance";
    public const string PhysicalDisk = "PhysicalDisk";
    public const string ReFS = "ReFS";
    public const string ReFSBucketizedPerformance = "ReFS Bucketized Performance";
    public const string ReFSDedupMinstorePerfCounters = "ReFS Dedup Minstore Perf Counters";
    public const string ReFSDedupPerfCounters = "ReFS Dedup Perf Counters";
    public const string StorageManagementWspSpacesRuntime = "Storage Management WSP Spaces Runtime";
    public const string StorageSpacesDrt = "Storage Spaces Drt";
    public const string StorageSpacesTier = "Storage Spaces Tier";
    public const string StorageSpacesVirtualDisk = "Storage Spaces Virtual Disk";
    public const string StorageSpacesVirtualDiskIo = "Storage Spaces Virtual Disk Io";
    public const string StorageSpacesVirtualDiskMap = "Storage Spaces Virtual Disk Map";
    public const string StorportUnitQueue = "Storport Unit Queue";
    public const string StorportUnitReads = "Storport Unit Reads";
    public const string StorportUnitTransfers = "Storport Unit Transfers";
    public const string StorportUnitWrites = "Storport Unit Writes";
    public const string VhdBucketizedPerformance = "VHD Bucketized Performance";

    public static IReadOnlyList<string> All { get; } =
    [
        FileSystemDiskActivity,
        LogicalDisk,
        NtfsBucketizedPerformance,
        PhysicalDisk,
        ReFS,
        ReFSBucketizedPerformance,
        ReFSDedupMinstorePerfCounters,
        ReFSDedupPerfCounters,
        StorageManagementWspSpacesRuntime,
        StorageSpacesDrt,
        StorageSpacesTier,
        StorageSpacesVirtualDisk,
        StorageSpacesVirtualDiskIo,
        StorageSpacesVirtualDiskMap,
        StorportUnitQueue,
        StorportUnitReads,
        StorportUnitTransfers,
        StorportUnitWrites,
        VhdBucketizedPerformance
    ];
}
