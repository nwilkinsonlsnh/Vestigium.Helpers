using System.Globalization;
using System.Net.NetworkInformation;
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

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(TcpipParameters);
            var raw = key?.GetValue("SearchList") as string;
            if (string.IsNullOrWhiteSpace(raw))
                raw = key?.GetValue("NV SearchList") as string;
            return SplitList(raw);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            return [];
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
        metricIsAutomatic = null;
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

            leaseObtained = ReadLeaseTime(key, "LeaseObtainedTime");
            leaseExpires = ReadLeaseTime(key, "LeaseTerminatesTime") ?? ReadLeaseTime(key, "Lease");
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
