using System.Net.NetworkInformation;

namespace Vestigium.Helpers.Network;

public sealed record WirelessAssociation(string Ssid, string Phy, int Quality, string? Bssid = null);

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
}
