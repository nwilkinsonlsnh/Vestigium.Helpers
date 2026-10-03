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
        if (!OperatingSystem.IsWindows())
            return [];

        var rows = new List<NetworkNetBiosName>();
        foreach (var lana in Lanas())
        {
            Reset(lana);
            rows.AddRange(LocalNames(lana));
        }

        return rows;
    }

    public static NetworkNetBiosStats ReadStats()
    {
        if (!OperatingSystem.IsWindows())
            return new NetworkNetBiosStats(0, 0, 0, 0, null);
        return new NetworkNetBiosStats(0, 0, 0, 0, NodeType());
    }

    private static IEnumerable<byte> Lanas()
    {
        var list = new LanaEnum { lana = new byte[254] };
        var ncb = Blank(NcbEnum);
        ncb.buffer = Marshal.AllocHGlobal(Marshal.SizeOf<LanaEnum>());
        ncb.length = (ushort)Marshal.SizeOf<LanaEnum>();
        try
        {
            Marshal.StructureToPtr(list, ncb.buffer, false);
            if (Netbios(ref ncb) != Good)
                yield break;
            list = Marshal.PtrToStructure<LanaEnum>(ncb.buffer);
            var count = list.length;
            for (var i = 0; i < count && i < list.lana.Length; i++)
                yield return list.lana[i];
        }
        finally
        {
            Marshal.FreeHGlobal(ncb.buffer);
        }
    }

    private static void Reset(byte lana)
    {
        var ncb = Blank(NcbReset);
        ncb.lana_num = lana;
        Netbios(ref ncb);
    }

    private static IEnumerable<NetworkNetBiosName> LocalNames(byte lana)
    {
        const int size = 4096;
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            var ncb = Blank(NcbAstat);
            ncb.lana_num = lana;
            ncb.buffer = buffer;
            ncb.length = size;
            ncb.callname[0] = (byte)'*';
            for (var i = 1; i < 16; i++)
                ncb.callname[i] = (byte)' ';
            if (Netbios(ref ncb) != Good)
                yield break;

            var adapter = AdapterLabel(lana);
            var node = NodeAddress(lana);
            var count = Marshal.ReadInt16(buffer, 58);
            if (count < 0 || count > 250)
                yield break;
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

    private static Ncb Blank(byte command)
        => new()
        {
            command = command,
            callname = new byte[16],
            name = new byte[16],
            reserve = new byte[10]
        };

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

    [DllImport("netapi32.dll", CharSet = CharSet.Ansi)]
    private static extern byte Netbios(ref Ncb ncb);

    [StructLayout(LayoutKind.Sequential)]
    private struct Ncb
    {
        public byte command;
        public byte retcode;
        public byte lsn;
        public byte num;
        public IntPtr buffer;
        public ushort length;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] callname;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] name;
        public byte rto;
        public byte sto;
        public IntPtr post;
        public byte lana_num;
        public byte cmd_cplt;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] reserve;
        public IntPtr eventHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LanaEnum
    {
        public byte length;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 254)]
        public byte[] lana;
    }
}
