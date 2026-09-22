using System.Net.NetworkInformation;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class NetworkNetBios
{
    public static NetBiosInfo Capture()
    {
        if (!OperatingSystem.IsWindows())
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Netbios, nameof(Capture), "NetBIOS is Windows-only");
            throw new PlatformNotSupportedException("GetNetBios is Windows-only. Linux is not emulated with nmblookup.");
        }

        var workstation = NetworkInventoryEngine.Capture();
        string? domain = null;
        try
        {
            domain = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            if (string.IsNullOrWhiteSpace(domain))
                domain = null;
        }
        catch (NetworkInformationException)
        {
        }

        var adapters = workstation.Adapters
            .Select(a => new NetBiosAdapterStatus(a.Name, a.NetbiosOverTcp, a.Description))
            .ToArray();
        return new NetBiosInfo(workstation.HostName, domain, adapters);
    }
}
