using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace Vestigium.Helpers.Network;

public sealed record WirelessAssociation
{
    public WirelessAssociation(
        string Ssid,
        string Phy,
        int Quality,
        string? Bssid = null,
        int? ReceiveKbps = null,
        int? TransmitKbps = null,
        string? Security = null)
    {
        this.Ssid = Ssid;
        this.Phy = Phy;
        this.Quality = Math.Clamp(Quality, 0, 100);
        this.Bssid = string.IsNullOrWhiteSpace(Bssid) ? null : Bssid;
        this.ReceiveKbps = ReceiveKbps is > 0 ? ReceiveKbps : null;
        this.TransmitKbps = TransmitKbps is > 0 ? TransmitKbps : null;
        this.Security = string.IsNullOrWhiteSpace(Security) ? null : Security;
    }

    public string Ssid { get; }

    public string Phy { get; }

    public int Quality { get; }

    public string? Bssid { get; }

    public int? ReceiveKbps { get; }

    public int? TransmitKbps { get; }

    public string? Security { get; }
}

public static partial class NetworkHelper
{
    public static WirelessAssociation? TryWirelessAssociation(NetworkAdapter? adapter)
    {
        if (adapter is null || adapter.Type != NetworkInterfaceType.Wireless80211)
            return null;

        if (string.IsNullOrWhiteSpace(adapter.Id))
            return null;

        try
        {
            return WirelessAssociationReader.Read(adapter.Id);
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}

internal static class WirelessAssociationReader
{
    private const uint CurrentConnection = 7;

    public static WirelessAssociation? Read(string adapterId)
    {
        if (!Guid.TryParse(adapterId.Trim('{', '}'), out var guid))
            return null;

        var handle = IntPtr.Zero;
        var list = IntPtr.Zero;
        try
        {
            if (WlanOpenHandle(2, IntPtr.Zero, out _, out handle) != 0)
                return null;
            if (WlanEnumInterfaces(handle, IntPtr.Zero, out list) != 0 || list == IntPtr.Zero)
                return null;

            var count = Marshal.ReadInt32(list);
            var item = list + 8;
            for (var i = 0; i < count; i++)
            {
                var info = Marshal.PtrToStructure<WlanInterfaceInfo>(item);
                item += Marshal.SizeOf<WlanInterfaceInfo>();
                if (info.InterfaceGuid != guid)
                    continue;

                return QueryConnection(handle, guid);
            }
        }
        finally
        {
            if (list != IntPtr.Zero)
                WlanFreeMemory(list);
            if (handle != IntPtr.Zero)
                WlanCloseHandle(handle, IntPtr.Zero);
        }

        return null;
    }

    private static WirelessAssociation? QueryConnection(IntPtr handle, Guid guid)
    {
        var data = IntPtr.Zero;
        try
        {
            if (WlanQueryInterface(handle, ref guid, CurrentConnection, IntPtr.Zero, out _, out data, out _) != 0
                || data == IntPtr.Zero)
            {
                return null;
            }

            var connection = Marshal.PtrToStructure<WlanConnectionAttributes>(data);
            return FromFields(
                ReadSsid(connection.Association.Ssid),
                connection.Association.PhyType,
                connection.Association.SignalQuality,
                connection.Association.Mac,
                connection.Association.RxRate,
                connection.Association.TxRate,
                SecurityLabel(connection.Security));
        }
        finally
        {
            if (data != IntPtr.Zero)
                WlanFreeMemory(data);
        }
    }

    public static WirelessAssociation? FromFields(
        string? ssid,
        uint phy,
        uint quality,
        byte[]? bssid,
        uint receiveKbps = 0,
        uint transmitKbps = 0,
        string? security = null)
    {
        if (string.IsNullOrWhiteSpace(ssid))
            return null;

        return new WirelessAssociation(
            ssid.Trim(),
            PhyName(phy),
            (int)quality,
            FormatBssid(bssid),
            receiveKbps == 0 ? null : (int)receiveKbps,
            transmitKbps == 0 ? null : (int)transmitKbps,
            security);
    }

    public static string SecurityLabel(bool enabled, bool oneX, uint auth, uint cipher)
    {
        if (!enabled)
            return "Open";

        var parts = new List<string> { AuthName(auth) };
        var cipherName = CipherName(cipher);
        if (cipherName is not null)
            parts.Add(cipherName);
        if (oneX)
            parts.Add("802.1X");
        return string.Join(" \u00b7 ", parts);
    }

    private static string SecurityLabel(WlanSecurityAttributes security)
        => SecurityLabel(security.SecurityEnabled, security.OneXEnabled, security.AuthAlgorithm, security.CipherAlgorithm);

    private static string AuthName(uint auth) => auth switch
    {
        1 => "Open",
        2 => "Shared",
        3 => "WPA",
        4 => "WPA-Personal",
        6 => "WPA2-Enterprise",
        7 => "WPA2-Personal",
        8 => "WPA3-Enterprise",
        9 => "WPA3-Personal",
        10 => "OWE",
        _ => "Secured"
    };

    private static string? CipherName(uint cipher) => cipher switch
    {
        2 => "TKIP",
        4 => "CCMP",
        8 or 9 => "GCMP",
        10 => "CCMP",
        _ => null
    };

    public static string PhyName(uint phy) => phy switch
    {
        4 or 5 => "802.11a",
        3 => "802.11b",
        6 => "802.11g",
        7 => "802.11n",
        8 => "802.11ac",
        10 => "802.11ax",
        11 => "802.11be",
        _ => "Wi-Fi"
    };

    public static string? FormatBssid(byte[]? mac)
    {
        if (mac is null || mac.Length < 6)
            return null;

        if (mac.Take(6).All(b => b == 0))
            return null;

        return string.Create(17, mac, static (span, source) =>
        {
            for (var i = 0; i < 6; i++)
            {
                if (i > 0)
                    span[i * 3 - 1] = ':';
                source[i].TryFormat(span[(i * 3)..], out _, "X2");
            }
        });
    }

    private static string ReadSsid(Dot11Ssid ssid)
    {
        var length = (int)Math.Clamp(ssid.Length, 0, 32);
        if (length == 0 || ssid.Bytes is null)
            return string.Empty;
        return Encoding.UTF8.GetString(ssid.Bytes, 0, length).Trim().Trim('\0');
    }

    [DllImport("wlanapi.dll")]
    private static extern uint WlanOpenHandle(uint clientVersion, IntPtr reserved, out uint negotiated, out IntPtr handle);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanCloseHandle(IntPtr handle, IntPtr reserved);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanEnumInterfaces(IntPtr handle, IntPtr reserved, out IntPtr list);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanQueryInterface(
        IntPtr handle,
        ref Guid interfaceGuid,
        uint opcode,
        IntPtr reserved,
        out uint dataSize,
        out IntPtr data,
        out uint opcodeValueType);

    [DllImport("wlanapi.dll")]
    private static extern void WlanFreeMemory(IntPtr memory);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WlanInterfaceInfo
    {
        public Guid InterfaceGuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Description;
        public uint State;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Dot11Ssid
    {
        public uint Length;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] Bytes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WlanAssociationAttributes
    {
        public Dot11Ssid Ssid;
        public uint BssType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] Mac;
        public uint PhyType;
        public uint PhyIndex;
        public uint SignalQuality;
        public uint RxRate;
        public uint TxRate;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WlanSecurityAttributes
    {
        [MarshalAs(UnmanagedType.Bool)] public bool SecurityEnabled;
        [MarshalAs(UnmanagedType.Bool)] public bool OneXEnabled;
        public uint AuthAlgorithm;
        public uint CipherAlgorithm;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WlanConnectionAttributes
    {
        public uint IsState;
        public uint ConnectionMode;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string ProfileName;
        public WlanAssociationAttributes Association;
        public WlanSecurityAttributes Security;
    }
}
