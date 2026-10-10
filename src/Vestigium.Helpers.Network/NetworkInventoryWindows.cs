using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Win32;

namespace Vestigium.Helpers.Network;

internal static class NetworkInventoryWindows
{
    private const string NetClass = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
    private const string TcpipParameters = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
    private const int NcfPhysical = 0x4;

    public static IReadOnlyList<string> ReadSearchList()
    {
        if (!OperatingSystem.IsWindows())
            return [];

        var found = new List<string>();
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(TcpipParameters);
            AddSplit(found, key?.GetValue("SearchList") as string);
            AddSplit(found, key?.GetValue("NV SearchList") as string);
            AddOne(found, key?.GetValue("Domain") as string);
            AddOne(found, key?.GetValue("DhcpDomain") as string);

            using var interfaces = Registry.LocalMachine.OpenSubKey(TcpipParameters + @"\Interfaces");
            if (interfaces is not null)
            {
                foreach (var name in interfaces.GetSubKeyNames())
                {
                    using var iface = interfaces.OpenSubKey(name);
                    AddSplit(found, iface?.GetValue("DhcpDomainSearchList") as string);
                    AddOne(found, iface?.GetValue("Domain") as string);
                    AddOne(found, iface?.GetValue("DhcpDomain") as string);
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
        }

        return found;
    }

    public static TcpipStackInfo? ReadStack()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(TcpipParameters);
            if (key is null)
                return null;

            var router = ReadDword(key, "IPEnableRouter");
            var sync = ReadDword(key, "SyncDomainWithMembership");
            return new TcpipStackInfo(
                Text(key, "Hostname") ?? Text(key, "NV Hostname"),
                Text(key, "Domain"),
                Text(key, "NV Domain"),
                Text(key, "DhcpDomain"),
                Text(key, "NameServer"),
                SplitList(key.GetValue("DhcpNameServer") as string),
                router is null ? null : router != 0,
                sync is null ? null : sync != 0);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            return null;
        }
    }

    public static void ReadInterface(
        NetworkInterface nic,
        out int? metric,
        out bool? metricIsAutomatic,
        out bool? autoconfig,
        out DateTimeOffset? leaseObtained,
        out DateTimeOffset? leaseExpires)
    {
        metric = null;
        metricIsAutomatic = OperatingSystem.IsWindows() ? true : null;
        autoconfig = null;
        leaseObtained = null;
        leaseExpires = null;
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            var id = nic.Id.Trim();
            var path = $@"{TcpipParameters}\Interfaces\{id}";
            using var key = Registry.LocalMachine.OpenSubKey(path);
            if (key is null)
                return;

            metric = ReadDword(key, "InterfaceMetric");
            var autoMetric = ReadDword(key, "AutoMetric");
            if (autoMetric is not null)
                metricIsAutomatic = autoMetric != 0;
            else
                metricIsAutomatic = metric is null;

            var autoCfg = ReadDword(key, "IPAutoconfigurationEnabled");
            if (autoCfg is not null)
                autoconfig = autoCfg != 0;
            else if (ReadDword(key, "EnableDHCP") == 1)
                autoconfig = true;

            leaseObtained = ReadLeaseTime(key, "LeaseObtainedTime");
            leaseExpires = ReadLeaseTime(key, "LeaseTerminatesTime");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
        }
    }

    public static AdapterDriver? ReadDriver(NetworkInterface nic, out bool? physical)
    {
        physical = null;
        if (!OperatingSystem.IsWindows())
            return null;

        try
        {
            var id = nic.Id;
            using var root = Registry.LocalMachine.OpenSubKey(NetClass);
            if (root is null)
                return null;

            foreach (var name in root.GetSubKeyNames())
            {
                if (name.Length == 0 || !char.IsDigit(name[0]))
                    continue;

                using var key = root.OpenSubKey(name);
                if (key is null)
                    continue;

                var instance = key.GetValue("NetCfgInstanceId") as string;
                if (string.IsNullOrWhiteSpace(instance)
                    || !string.Equals(instance, id, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var characteristics = ReadDword(key, "Characteristics");
                if (characteristics is not null)
                    physical = (characteristics.Value & NcfPhysical) != 0;

                var dateRaw = key.GetValue("DriverDate") as string;
                return new AdapterDriver(
                    Text(key, "ProviderName"),
                    Text(key, "DriverVersion"),
                    ParseDriverDate(dateRaw),
                    Text(key, "DriverDesc"),
                    Text(key, "InfPath"),
                    Text(key, "MatchingDeviceId") ?? Text(key, "DeviceInstanceID"),
                    Text(key, "Service"));
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
        }

        return null;
    }

    private static void AddSplit(List<string> target, string? raw)
    {
        foreach (var item in SplitList(raw))
            AddOne(target, item);
    }

    private static void AddOne(List<string> target, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        var item = value.Trim().Trim('.');
        if (item.Length == 0)
            return;
        if (!target.Exists(x => string.Equals(x, item, StringComparison.OrdinalIgnoreCase)))
            target.Add(item);
    }

    private static IReadOnlyList<string> SplitList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw
            .Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int? ReadDword(RegistryKey key, string name)
        => key.GetValue(name) is int value ? value : null;

    private static string? Text(RegistryKey key, string name)
    {
        var value = key.GetValue(name) as string;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTimeOffset? ReadLeaseTime(RegistryKey key, string name)
    {
        var raw = key.GetValue(name);
        if (raw is not int dword || dword <= 0)
            return null;

        try
        {
            // Modern stacks store Unix seconds. Older DHCP client used 1980-01-01.
            if (dword >= 1_500_000_000)
                return DateTimeOffset.FromUnixTimeSeconds(dword);
            return new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(dword);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static DateTimeOffset? ParseDriverDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var text = raw.Trim();
        string[] formats = ["M-d-yyyy", "MM-dd-yyyy", "yyyy-MM-dd", "M/d/yyyy", "MM/dd/yyyy"];
        if (DateTimeOffset.TryParseExact(
                text,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var exact))
        {
            return exact;
        }

        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var loose)
            ? loose
            : null;
    }
}

    public static IReadOnlyList<UnicastAddress> ReadInterfaceAddresses(string id)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(id))
            return [];

        var found = new List<UnicastAddress>();
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(TcpipParameters + @"\Interfaces\" + id);
            Add(found, key?.GetValue("DhcpIPAddress"), AddressFamily.InterNetwork, true);
            Add(found, key?.GetValue("IPAddress"), AddressFamily.InterNetwork, false);
        }
        catch (Exception)
        {
        }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces\" + id);
            Add(found, key?.GetValue("DhcpIPAddress"), AddressFamily.InterNetworkV6, true);
            Add(found, key?.GetValue("IPAddress"), AddressFamily.InterNetworkV6, false);
        }
        catch (Exception)
        {
        }

        return found
            .GroupBy(a => a.Address, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToArray();
    }

    private static void Add(List<UnicastAddress> found, object? value, AddressFamily family, bool dhcp)
    {
        foreach (var raw in Values(value))
        {
            if (!IPAddress.TryParse(raw, out var ip) || IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any))
                continue;
            if (ip.AddressFamily != family)
                continue;
            found.Add(new UnicastAddress(family, ip.ToString(), null, null, dhcp));
        }
    }

    private static IEnumerable<string> Values(object? value)
    {
        if (value is string one && !string.IsNullOrWhiteSpace(one))
        {
            yield return one.Trim();
            yield break;
        }

        if (value is string[] many)
        {
            foreach (var item in many)
            {
                if (!string.IsNullOrWhiteSpace(item))
                    yield return item.Trim();
            }
        }
    }
