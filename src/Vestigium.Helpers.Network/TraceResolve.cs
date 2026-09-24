using System.Net;
using System.Net.Sockets;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class TraceResolve
{
    public static void Guard(RouteFamily family)
    {
        if (family is RouteFamily.All or RouteFamily.Pv4 or RouteFamily.Pv6)
            return;
        HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"Family={family}");
        throw new ArgumentOutOfRangeException(nameof(family), "Family must be All, Pv4, or Pv6.");
    }

    public static AddressFamily? Pin(RouteFamily family)
        => family switch
        {
            RouteFamily.Pv4 => AddressFamily.InterNetwork,
            RouteFamily.Pv6 => AddressFamily.InterNetworkV6,
            _ => null
        };

    public static void GuardLiteral(string target, RouteFamily family)
    {
        if (!IPAddress.TryParse(target, out var ip))
            return;
        var pin = Pin(family);
        if (pin is null)
            return;
        if (ip.AddressFamily == pin)
            return;
        HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(GuardLiteral), $"target family={ip.AddressFamily} pin={family}");
        throw new ArgumentException("Target address is not in the pinned family.", nameof(target));
    }

    public static async Task<string> ResolveAsync(string target, RouteFamily family, CancellationToken token)
    {
        if (IPAddress.TryParse(target, out var parsed))
        {
            GuardLiteral(target, family);
            return parsed.ToString();
        }

        var pin = Pin(family);
        if (pin is null)
            return target;

        try
        {
            var addrs = await Dns.GetHostAddressesAsync(target, pin.Value, token).ConfigureAwait(false);
            var dest = addrs.FirstOrDefault(a => a.AddressFamily == pin.Value);
            if (dest is not null)
                return dest.ToString();
        }
        catch (SocketException)
        {
        }
        catch (ArgumentException)
        {
        }

        return target;
    }

    public static IPAddress? Pick(IPAddress[] addrs, RouteFamily family)
    {
        var pin = Pin(family);
        if (pin is { } required)
            return addrs.FirstOrDefault(a => a.AddressFamily == required);
        return addrs.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
            ?? addrs.FirstOrDefault();
    }
}
