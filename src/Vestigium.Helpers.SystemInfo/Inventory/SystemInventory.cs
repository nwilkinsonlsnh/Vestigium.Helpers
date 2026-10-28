using System.Management;

namespace Vestigium.Helpers.SystemInfo;

/// <summary>Inventory doors. Structured records. No text, no files.</summary>
public static partial class SystemInventory
{
    /// <summary>
    /// Reads ComputerSystem, OperatingSystem, BIOS, and Processor.
    /// Each section is Fact.Unavailable on failure. Partial success is allowed.
    /// </summary>
    public static BaseInventory ReadBase()
    {
        return new BaseInventory(
            ReadComputerSystem(),
            ReadOperatingSystem(),
            ReadBios(),
            ReadProcessor());
    }

    private static Fact<ComputerSystemInfo> ReadComputerSystem()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT DNSHostName, Name, Domain, DomainRole, Manufacturer, Model, NumberOfProcessors, NumberOfLogicalProcessors, TotalPhysicalMemory, UserName FROM Win32_ComputerSystem");
            using var results = searcher.Get();
            foreach (ManagementObject mo in results)
            {
                using (mo)
                {
                    var hostname = String(mo, "DNSHostName") ?? String(mo, "Name") ?? Environment.MachineName;
                    return Fact<ComputerSystemInfo>.Ok(new ComputerSystemInfo(
                        hostname,
                        String(mo, "Domain") ?? "",
                        Int(mo, "DomainRole"),
                        String(mo, "Manufacturer") ?? "",
                        String(mo, "Model") ?? "",
                        Int(mo, "NumberOfProcessors"),
                        Int(mo, "NumberOfLogicalProcessors"),
                        ULong(mo, "TotalPhysicalMemory"),
                        String(mo, "UserName") ?? ""));
                }
            }
            return Fact<ComputerSystemInfo>.Unavailable();
        }
        catch (Exception)
        {
            return Fact<ComputerSystemInfo>.Unavailable();
        }
    }

    private static Fact<OperatingSystemInfo> ReadOperatingSystem()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProductType, Caption, OSArchitecture, Version, BuildNumber, InstallDate, LastBootUpTime FROM Win32_OperatingSystem");
            using var results = searcher.Get();
            foreach (ManagementObject mo in results)
            {
                using (mo)
                {
                    return Fact<OperatingSystemInfo>.Ok(new OperatingSystemInfo(
                        Int(mo, "ProductType"),
                        String(mo, "Caption") ?? "",
                        String(mo, "OSArchitecture") ?? "",
                        String(mo, "Version") ?? "",
                        String(mo, "BuildNumber") ?? "",
                        Date(mo, "InstallDate"),
                        Date(mo, "LastBootUpTime")));
                }
            }
            return Fact<OperatingSystemInfo>.Unavailable();
        }
        catch (Exception)
        {
            return Fact<OperatingSystemInfo>.Unavailable();
        }
    }

    private static Fact<BiosInfo> ReadBios()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
            using var results = searcher.Get();
            foreach (ManagementObject mo in results)
            {
                using (mo)
                    return Fact<BiosInfo>.Ok(new BiosInfo(String(mo, "SerialNumber") ?? ""));
            }
            return Fact<BiosInfo>.Unavailable();
        }
        catch (Exception)
        {
            return Fact<BiosInfo>.Unavailable();
        }
    }

    private static Fact<ProcessorInfo> ReadProcessor()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            using var results = searcher.Get();
            foreach (ManagementObject mo in results)
            {
                using (mo)
                    return Fact<ProcessorInfo>.Ok(new ProcessorInfo(String(mo, "Name")?.Trim() ?? ""));
            }
            return Fact<ProcessorInfo>.Unavailable();
        }
        catch (Exception)
        {
            return Fact<ProcessorInfo>.Unavailable();
        }
    }

    private static string? String(ManagementObject mo, string name)
        => mo[name]?.ToString();

    private static int Int(ManagementObject mo, string name)
        => int.TryParse(mo[name]?.ToString(), out var n) ? n : 0;

    private static uint UInt(ManagementObject mo, string name)
        => uint.TryParse(mo[name]?.ToString(), out var n) ? n : 0;

    private static ulong ULong(ManagementObject mo, string name)
        => ulong.TryParse(mo[name]?.ToString(), out var n) ? n : 0;

    private static bool Bool(ManagementObject mo, string name)
        => bool.TryParse(mo[name]?.ToString(), out var b) && b;

    private static DateTime? Date(ManagementObject mo, string name)
    {
        var raw = mo[name]?.ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        try
        {
            return ManagementDateTimeConverter.ToDateTime(raw);
        }
        catch
        {
            return DateTime.TryParse(raw, out var d) ? d : null;
        }
    }
}
