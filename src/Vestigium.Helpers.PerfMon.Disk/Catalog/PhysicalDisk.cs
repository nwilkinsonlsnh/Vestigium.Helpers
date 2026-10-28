namespace Vestigium.Helpers.PerfMon.Disk;

/// <summary>PDH category PhysicalDisk. Generated from EventCatalog/pdh-categories.json.</summary>
public static class PhysicalDisk
{
    public const string Category = "PhysicalDisk";
    public const string PercentDiskReadTime = "% Disk Read Time";
    public const string PercentDiskTime = "% Disk Time";
    public const string PercentDiskWriteTime = "% Disk Write Time";
    public const string PercentIdleTime = "% Idle Time";
    public const string AvgDiskBytesRead = "Avg. Disk Bytes/Read";
    public const string AvgDiskBytesTransfer = "Avg. Disk Bytes/Transfer";
    public const string AvgDiskBytesWrite = "Avg. Disk Bytes/Write";
    public const string AvgDiskQueueLength = "Avg. Disk Queue Length";
    public const string AvgDiskReadQueueLength = "Avg. Disk Read Queue Length";
    public const string AvgDiskSecRead = "Avg. Disk sec/Read";
    public const string AvgDiskSecTransfer = "Avg. Disk sec/Transfer";
    public const string AvgDiskSecWrite = "Avg. Disk sec/Write";
    public const string AvgDiskWriteQueueLength = "Avg. Disk Write Queue Length";
    public const string CurrentDiskQueueLength = "Current Disk Queue Length";
    public const string DiskBytesPerSec = "Disk Bytes/sec";
    public const string DiskReadBytesPerSec = "Disk Read Bytes/sec";
    public const string DiskReadsPerSec = "Disk Reads/sec";
    public const string DiskTransfersPerSec = "Disk Transfers/sec";
    public const string DiskWriteBytesPerSec = "Disk Write Bytes/sec";
    public const string DiskWritesPerSec = "Disk Writes/sec";
    public const string SplitIOPerSec = "Split IO/Sec";

    public static IReadOnlyList<string> Counters { get; } =
    [
        PercentDiskReadTime,
        PercentDiskTime,
        PercentDiskWriteTime,
        PercentIdleTime,
        AvgDiskBytesRead,
        AvgDiskBytesTransfer,
        AvgDiskBytesWrite,
        AvgDiskQueueLength,
        AvgDiskReadQueueLength,
        AvgDiskSecRead,
        AvgDiskSecTransfer,
        AvgDiskSecWrite,
        AvgDiskWriteQueueLength,
        CurrentDiskQueueLength,
        DiskBytesPerSec,
        DiskReadBytesPerSec,
        DiskReadsPerSec,
        DiskTransfersPerSec,
        DiskWriteBytesPerSec,
        DiskWritesPerSec,
        SplitIOPerSec,
    ];
}
