using System.Management;

namespace Vestigium.Helpers.SystemInfo;

public static partial class SystemInventory
{
    private const string StorageScope = @"\\.\root\Microsoft\Windows\Storage";

    /// <summary>
    /// Reads disk drives, logical disks, partitions, and volumes.
    /// Each section is Fact.Unavailable on failure. Partial success is allowed.
    /// Partitions and volumes prefer the Storage namespace and fall back to Win32.
    /// </summary>
    public static StorageInventory ReadStorage()
    {
        return new StorageInventory(
            ReadDiskDrives(),
            ReadLogicalDisks(),
            ReadPartitions(),
            ReadVolumes());
    }

    private static Fact<IReadOnlyList<DiskDrive>> ReadDiskDrives()
    {
        try
        {
            var list = new List<DiskDrive>();
            foreach (var mo in Query("SELECT DeviceID, Caption, InterfaceType, Size, SerialNumber, Status, Partitions, MediaType FROM Win32_DiskDrive"))
            {
                using (mo)
                {
                    list.Add(new DiskDrive(
                        String(mo, "DeviceID") ?? "",
                        String(mo, "Caption") ?? "",
                        String(mo, "InterfaceType") ?? "",
                        ULong(mo, "Size"),
                        (String(mo, "SerialNumber") ?? "").Trim(),
                        String(mo, "Status") ?? "",
                        UInt(mo, "Partitions"),
                        String(mo, "MediaType") ?? ""));
                }
            }
            return Fact<IReadOnlyList<DiskDrive>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<DiskDrive>>.Unavailable();
        }
    }

    private static Fact<IReadOnlyList<LogicalDisk>> ReadLogicalDisks()
    {
        try
        {
            var list = new List<LogicalDisk>();
            foreach (var mo in Query("SELECT DeviceID, VolumeName, FileSystem, DriveType, Size, FreeSpace, VolumeSerialNumber FROM Win32_LogicalDisk"))
            {
                using (mo)
                {
                    list.Add(new LogicalDisk(
                        String(mo, "DeviceID") ?? "",
                        String(mo, "VolumeName") ?? "",
                        String(mo, "FileSystem") ?? "",
                        UInt(mo, "DriveType"),
                        ULong(mo, "Size"),
                        ULong(mo, "FreeSpace"),
                        String(mo, "VolumeSerialNumber") ?? ""));
                }
            }
            return Fact<IReadOnlyList<LogicalDisk>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<LogicalDisk>>.Unavailable();
        }
    }

    private static Fact<IReadOnlyList<Partition>> ReadPartitions()
    {
        var preferred = TryStorage(
            "SELECT DiskNumber, PartitionNumber, DriveLetter, Size, Offset, GptType, IsBoot, IsSystem, IsHidden, OperationalStatus FROM MSFT_Partition",
            MapStoragePartition);
        if (preferred is not null)
            return Fact<IReadOnlyList<Partition>>.Ok(preferred);

        try
        {
            var list = new List<Partition>();
            foreach (var mo in Query("SELECT DiskIndex, Index, Type, Size, StartingOffset, BootPartition, PrimaryPartition, Status FROM Win32_DiskPartition"))
            {
                using (mo)
                {
                    list.Add(new Partition(
                        UInt(mo, "DiskIndex"),
                        UInt(mo, "Index"),
                        "",
                        String(mo, "Type") ?? "",
                        ULong(mo, "Size"),
                        ULong(mo, "StartingOffset"),
                        Bool(mo, "BootPartition"),
                        Bool(mo, "PrimaryPartition"),
                        false,
                        String(mo, "Status") ?? ""));
                }
            }
            return Fact<IReadOnlyList<Partition>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<Partition>>.Unavailable();
        }
    }

    private static Fact<IReadOnlyList<Volume>> ReadVolumes()
    {
        var preferred = TryStorage(
            "SELECT DriveLetter, FileSystemLabel, FileSystem, DriveType, Size, SizeRemaining, HealthStatus, OperationalStatus, Path FROM MSFT_Volume",
            MapStorageVolume);
        if (preferred is not null)
            return Fact<IReadOnlyList<Volume>>.Ok(preferred);

        try
        {
            var list = new List<Volume>();
            foreach (var mo in Query("SELECT DriveLetter, Label, FileSystem, DriveType, Capacity, FreeSpace, DeviceID FROM Win32_Volume"))
            {
                using (mo)
                {
                    list.Add(new Volume(
                        String(mo, "DriveLetter") ?? "",
                        String(mo, "Label") ?? "",
                        String(mo, "FileSystem") ?? "",
                        UInt(mo, "DriveType"),
                        ULong(mo, "Capacity"),
                        ULong(mo, "FreeSpace"),
                        "",
                        "",
                        String(mo, "DeviceID") ?? ""));
                }
            }
            return Fact<IReadOnlyList<Volume>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<Volume>>.Unavailable();
        }
    }

    private static Partition MapStoragePartition(ManagementObject mo) => new(
        UInt(mo, "DiskNumber"),
        UInt(mo, "PartitionNumber"),
        String(mo, "DriveLetter") ?? "",
        String(mo, "GptType") ?? "",
        ULong(mo, "Size"),
        ULong(mo, "Offset"),
        Bool(mo, "IsBoot"),
        Bool(mo, "IsSystem"),
        Bool(mo, "IsHidden"),
        String(mo, "OperationalStatus") ?? "");

    private static Volume MapStorageVolume(ManagementObject mo) => new(
        String(mo, "DriveLetter") ?? "",
        String(mo, "FileSystemLabel") ?? "",
        String(mo, "FileSystem") ?? "",
        UInt(mo, "DriveType"),
        ULong(mo, "Size"),
        ULong(mo, "SizeRemaining"),
        String(mo, "HealthStatus") ?? "",
        String(mo, "OperationalStatus") ?? "",
        String(mo, "Path") ?? "");

    private static List<T>? TryStorage<T>(string wql, Func<ManagementObject, T> map)
    {
        try
        {
            var scope = new ManagementScope(StorageScope);
            scope.Connect();
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(wql));
            using var results = searcher.Get();
            var list = new List<T>();
            foreach (ManagementObject mo in results)
            {
                using (mo)
                    list.Add(map(mo));
            }
            return list;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static IEnumerable<ManagementObject> Query(string wql)
    {
        using var searcher = new ManagementObjectSearcher(wql);
        using var results = searcher.Get();
        foreach (ManagementObject mo in results)
            yield return mo;
    }
}
