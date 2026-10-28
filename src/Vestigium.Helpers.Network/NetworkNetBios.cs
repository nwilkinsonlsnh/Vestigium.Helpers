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
    private const byte EnvNotDefined = 0x34;
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
        var notes = new List<string>();
        foreach (var lana in lanas)
        {
            var (found, note) = LocalNames(lana);
            rows.AddRange(found);
            notes.Add(note);
        }

        if (rows.Count == 0)
            _fault = string.Join("; ", notes);
        return rows;
    }

    public static NetworkNetBiosStats ReadStats()
        => new(0, 0, 0, 0, _fault ?? NodeType());

    private static IReadOnlyList<byte> Lanas()
    {
        var buffer = Marshal.AllocHGlobal(512);
        try
        {
            Zero(buffer, 512);
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

    private static (List<NetworkNetBiosName> Rows, string Note) LocalNames(byte lana)
    {
        const int size = 8192;
        var buffer = Marshal.AllocHGlobal(size);
        var rows = new List<NetworkNetBiosName>();
        try
        {
            Zero(buffer, size);
            var code = Call(NcbAstat, lana, buffer, size, Star());
            if (code == EnvNotDefined)
            {
                var reset = Reset(lana);
                if (reset != Good)
                    return (rows, "LANA " + lana + " reset 0x" + reset.ToString("X2"));
                Zero(buffer, size);
                code = Call(NcbAstat, lana, buffer, size, Star());
            }

            if (code != Good)
                return (rows, "LANA " + lana + " astat 0x" + code.ToString("X2"));

            var counted = Marshal.ReadInt16(buffer, 58);
            var count = counted is > 0 and <= 250 ? counted : Guess(buffer, size);
            var adapter = AdapterLabel(lana);
            var node = NodeAddress(lana);
            var cursor = buffer + 60;
            for (var i = 0; i < count; i++)
            {
                var raw = new byte[16];
                Marshal.Copy(cursor, raw, 0, 16);
                if (!LooksNamed(raw))
                    break;
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
                rows.Add(new NetworkNetBiosName("Local", name, suffix, SuffixName(suffix), group ? "GROUP" : "UNIQUE", status, null, null, adapter, node, false));
                cursor += 18;
            }

            return (rows, "LANA " + lana + " count " + counted + " mac " + Mac(buffer));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static byte Reset(byte lana)
    {
        var slots = new byte[16];
        slots[0] = 32;
        slots[2] = 32;
        return Call(NcbReset, lana, IntPtr.Zero, 0, slots);
    }

    private static int Guess(IntPtr buffer, int size)
    {
        var count = 0;
        var cursor = buffer + 60;
        while (count < 64 && cursor + 18 <= buffer + size)
        {
            var raw = new byte[16];
            Marshal.Copy(cursor, raw, 0, 16);
            if (!LooksNamed(raw))
                break;
            count++;
            cursor += 18;
        }

        return count;
    }

    private static bool LooksNamed(byte[] raw)
    {
        var letters = 0;
        for (var i = 0; i < 15; i++)
        {
            if (raw[i] is (>= 32 and <= 126))
                letters++;
        }

        return letters >= 2;
    }

    private static byte[] Star()
    {
        var callname = new byte[16];
        callname[0] = (byte)'*';
        for (var i = 1; i < 16; i++)
            callname[i] = (byte)' ';
        return callname;
    }

    private static string Mac(IntPtr buffer)
    {
        var raw = new byte[6];
        Marshal.Copy(buffer, raw, 0, 6);
        return Convert.ToHexString(raw);
    }

    private static void Zero(IntPtr buffer, int size)
    {
        for (var i = 0; i < size; i++)
            Marshal.WriteByte(buffer, i, 0);
    }

    private static byte Call(byte command, byte lana, IntPtr buffer, int length, byte[]? callname)
    {
        var ncb = new Ncb
        {
            command = command,
            buffer = buffer,
            length = (ushort)length,
            callname = callname ?? new byte[16],
            name = new byte[16],
            reserve = new byte[10],
            lana_num = lana
        };
        var size = Marshal.SizeOf<Ncb>();
        var ptr = Marshal.AllocHGlobal(size + 64);
        try
        {
            Zero(ptr, size + 64);
            Marshal.StructureToPtr(ncb, ptr, false);
            return Netbios(ptr);
        }
        finally
        {
            Marshal.DestroyStructure<Ncb>(ptr);
            Marshal.FreeHGlobal(ptr);
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
}
