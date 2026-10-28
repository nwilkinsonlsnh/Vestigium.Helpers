using System.Net;
using System.Net.Sockets;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal readonly record struct NetworkRouteSpec(
    IPAddress Destination,
    IPAddress Gateway,
    int PrefixLength,
    int InterfaceIndex,
    int Metric)
{
    public bool IsIPv6 => Destination.AddressFamily == AddressFamily.InterNetworkV6;
    public bool IsDefault => PrefixLength == 0 && (Destination.Equals(IPAddress.Any) || Destination.Equals(IPAddress.IPv6Any));

    public static NetworkRouteSpec Parse(NetworkRouteChange change)
    {
        var destText = HelperGuard.NotBlank(change.Destination, nameof(change.Destination));
        var gwText = HelperGuard.NotBlank(change.Gateway, nameof(change.Gateway));
        if (!IPAddress.TryParse(destText, out var dest))
            throw new ArgumentException("Destination must be an IP address.", nameof(change.Destination));
        if (!IPAddress.TryParse(gwText, out var gw))
            throw new ArgumentException("Gateway must be an IP address.", nameof(change.Gateway));
        if (dest.AddressFamily != gw.AddressFamily)
            throw new ArgumentException("Destination and gateway must be the same address family.", nameof(change.Gateway));

        var max = dest.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
        if (change.PrefixLength < 0 || change.PrefixLength > max)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Route, nameof(Parse), $"prefix={change.PrefixLength}");
            throw new ArgumentOutOfRangeException(nameof(change.PrefixLength), $"PrefixLength must be 0–{max}.");
        }

        var spec = new NetworkRouteSpec(dest, gw, change.PrefixLength, change.InterfaceIndex ?? 0, Math.Max(1, change.Metric));
        if (!spec.IsDefault) return spec;
        NetworkLog.RouteDenied(nameof(Parse), "default route");
        throw new NetworkRouteDenied("Default route write is not offered.");

    }
}
