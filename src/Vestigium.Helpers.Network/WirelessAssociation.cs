using System.Net.NetworkInformation;

namespace Vestigium.Helpers.Network;

public sealed record WirelessAssociation
{
    public WirelessAssociation(string Ssid, string Phy, int Quality, string? Bssid = null)
    {
        this.Ssid = Ssid;
        this.Phy = Phy;
        this.Quality = Math.Clamp(Quality, 0, 100);
        this.Bssid = string.IsNullOrWhiteSpace(Bssid) ? null : Bssid;
    }

    public string Ssid { get; }

    public string Phy { get; }

    public int Quality { get; }

    public string? Bssid { get; }
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
    public static WirelessAssociation? Read(string adapterId) => null;

    public static WirelessAssociation? FromFields(string? ssid, uint phy, uint quality, byte[]? bssid)
    {
        if (string.IsNullOrWhiteSpace(ssid))
            return null;

        return new WirelessAssociation(ssid.Trim(), PhyName(phy), (int)quality, FormatBssid(bssid));
    }

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
}
