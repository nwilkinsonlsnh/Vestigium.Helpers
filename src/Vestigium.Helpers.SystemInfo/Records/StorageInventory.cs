namespace Vestigium.Helpers.SystemInfo;

/// <summary>Storage inventory. Raw numbers. No display strings. Host formats.</summary>
public readonly record struct StorageInventory(
    Fact<IReadOnlyList<DiskDrive>> DiskDrives,
    Fact<IReadOnlyList<LogicalDisk>> LogicalDisks,
    Fact<IReadOnlyList<Partition>> Partitions,
    Fact<IReadOnlyList<Volume>> Volumes);

/// <summary>Win32_DiskDrive. Size is bytes.</summary>
public readonly record struct DiskDrive(
    string DeviceId,
    string Model,
    string InterfaceType,
    ulong SizeBytes,
    string SerialNumber,
    string Status,
    uint Partitions,
    string MediaType);

/// <summary>Win32_LogicalDisk. Size and Free are bytes. DriveType is the raw CIM value.</summary>
public readonly record struct LogicalDisk(
    string DeviceId,
    string VolumeName,
    string FileSystem,
    uint DriveType,
    ulong SizeBytes,
    ulong FreeBytes,
    string VolumeSerial);

/// <summary>Partition. Prefer MSFT_Partition; fall back to Win32_DiskPartition. Size and Offset are bytes.</summary>
public readonly record struct Partition(
    uint DiskNumber,
    uint PartitionNumber,
    string DriveLetter,
    string Type,
    ulong SizeBytes,
    ulong OffsetBytes,
    bool IsBoot,
    bool IsSystem,
    bool IsHidden,
    string Status);

/// <summary>Volume. Prefer MSFT_Volume; fall back to Win32_Volume. Size and Free are bytes. DriveType is raw.</summary>
public readonly record struct Volume(
    string DriveLetter,
    string Label,
    string FileSystem,
    uint DriveType,
    ulong SizeBytes,
    ulong FreeBytes,
    string Health,
    string Status,
    string Path);
