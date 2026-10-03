using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace Vestigium.Helpers.Network;

internal static class NetworkNetBios
{
    private const byte NcbReset = 0x32;
    private const byte NcbAstat = 0x33;
    private const byte NcbEnum = 0x37;
    private const byte Good = 0x00;
    private const int NcbSize = 76;
    private static string? _fault;

    public static NetBiosInfo Capture()
    {
        var workstation = NetworkInventoryEngine.Capture();
        var domain = IPGlobalProperties.GetIPGlobalProperties().DomainName;
        var adapters = workstation.Adapters
            .Select(adapter => new NetBiosAdapterStatus(adapter.Name, NetbiosOverTcp.Unknown, adapter.Description))
            .ToArray();
        return new NetBiosInfo(workstation.HostName, string.IsNullOrWhiteSpace(domain) ? null : domain, adapters);
    }

    public static IReadOnlyList<NetworkNetBiosName> Read()
    {
        _fault = null;
        if (!OperatingSystem.IsWindows())
        {
            _fault = "not Windows";
            return [];
        }

        var lanas = Lanas();
        if (lanas.Count == 0)
        {
            _fault ??= "no LANA";
            return [];
        }

        var rows = new List<NetworkNetBiosName>();
        foreach (var lana in lanas)
        {
            var reset = Call(NcbReset, lana, IntPtr.Zero, 0, null);
            if (reset != Good)
                _fault = "LANA " + lana + " reset 0x" + reset.ToString("X2");
            rows.AddRange(LocalNames(lana));
        }

        if (rows.Count == 0)
            _fault ??= "no names";
        return rows;
    }

    public static NetworkNetBiosStats ReadStats()
        => new(0, 0, 0, 0, _fault ?? NodeType());

    private static IReadOnlyList<byte> Lanas()
    {
        var buffer = Marshal.AllocHGlobal(255);
        try
        {
            var code = Call(NcbEnum, 0, buffer, 255, null);
            if (code == 0x01)
                code = Call(NcbEnum, 0, buffer, 256, null);
            if (code != Good)
            {
                _fault = "enum 0x" + code.ToString("X2");
                return [];
            }

            var count = Marshal.ReadByte(buffer);
            var found = new List<byte>(count);
            for (var i = 0; i < count && i < 254; i++)
                found.Add(Marshal.ReadByte(buffer, 1 + i));
            return found;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IEnumerable<NetworkNetBiosName> LocalNames(byte lana)
    {
        const int size = 4096;
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            var callname = new byte[16];
            callname[0] = (byte)'*';
            for (var i = 1; i < 16; i++)
                callname[i] = (byte)' ';
            var code = Call(NcbAstat, lana, buffer, size, callname);
            if (code != Good)
            {
                _fault = "LANA " + lana + " astat 0x" + code.ToString("X2");
                yield break;
            }

            var adapter = AdapterLabel(lana);
            var node = NodeAddress(lana);
            var count = Marshal.ReadInt16(buffer, 58);
            if (count < 0 || count > 250)
            {
                _fault = "LANA " + lana + " count " + count;
                yield break;
            }

            var cursor = buffer + 60;
            for (var i = 0; i < count; i++)
            {
                var raw = new byte[16];
                Marshal.Copy(cursor, raw, 0, 16);
                var suffix = raw[15].ToString("X2");
                var name = Encoding.ASCII.GetString(raw, 0, 15).Trim().TrimEnd('\0', ' ');
                var flags = Marshal.ReadByte(cursor, 17);
                var group = (flags & 0x80) != 0;
                var status = (flags & 0x07) switch
                {
                    0x04 => "Registered",
                    0x05 => "Deregistered",
                    0x06 => "Duplicate",
                    0x07 => "Duplicate deregistered",
                    _ => "Registering"
                };
                yield return new NetworkNetBiosName("Local", name, suffix, SuffixName(suffix), group ? "GROUP" : "UNIQUE", status, null, null, adapter, node, false);
                cursor += 18;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static byte Call(byte command, byte lana, IntPtr buffer, int length, byte[]? callname)
    {
        var ncb = Marshal.AllocHGlobal(NcbSize);
        try
        {
            for (var i = 0; i < NcbSize; i++)
                Marshal.WriteByte(ncb, i, 0);
            Marshal.WriteByte(ncb, 0, command);
            Marshal.WriteIntPtr(ncb, 4, buffer);
            Marshal.WriteInt16(ncb, 12, (short)length);
            if (callname is not null)
                Marshal.Copy(callname, 0, ncb + 14, 16);
            Marshal.WriteByte(ncb, 56, lana);
            return Netbios(ncb);
        }
        finally
        {
            Marshal.FreeHGlobal(ncb);
        }
    }

    private static string? AdapterLabel(byte lana)
    {
        var adapters = NetworkInterface.GetAllNetworkInterfaces().ToArray();
        return lana < adapters.Length ? adapters[lana].Name : "LANA " + lana;
    }

    private static string? NodeAddress(byte lana)
    {
        var adapters = NetworkInterface.GetAllNetworkInterfaces().ToArray();
        if (lana >= adapters.Length)
            return null;
        return adapters[lana].GetIPProperties().UnicastAddresses
            .Select(row => row.Address.ToString())
            .FirstOrDefault(text => text.Contains('.'));
    }

    private static string? NodeType()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\NetBT\Parameters");
            return key?.GetValue("NodeType") switch
            {
                int code => code switch { 1 => "B-node", 2 => "P-node", 4 => "M-node", 8 => "H-node", _ => code.ToString() },
                _ => null
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string SuffixName(string suffix)
        => suffix.ToUpperInvariant() switch
        {
            "00" => "Workstation",
            "01" => "Messenger",
            "03" => "Messenger",
            "06" => "RAS Server",
            "1B" => "Domain Master",
            "1C" => "Domain Controller",
            "1D" => "Master Browser",
            "1E" => "Browser Election",
            "1F" => "NetDDE",
            "20" => "File Server",
            "21" => "RAS Client",
            "BE" => "Network Monitor Agent",
            "BF" => "Network Monitor",
            _ => "Other"
        };

    [DllImport("netapi32.dll")]
    private static extern byte Netbios(IntPtr ncb);
}
