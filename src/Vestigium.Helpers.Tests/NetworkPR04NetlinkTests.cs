using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR04NetlinkTests
{
    [Fact]
    public void PR04_004_default_route_is_not_offered()
    {
        if (OperatingSystem.IsWindows())
            return;

        var ex = Assert.Throws<NetworkRouteDenied>(() => NetworkHelper.AddRoute(new NetworkRouteChange
        {
            Destination = "0.0.0.0",
            PrefixLength = 0,
            Gateway = "192.0.2.1",
            InterfaceIndex = 1
        }));
        Assert.Contains("Default route", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PR04_004_linux_ipv4_is_netlink_or_cap_deny()
    {
        if (OperatingSystem.IsWindows())
        {
            var denied = NetworkRouteNetlink.Denied("Add", 1);
            Assert.Contains("CAP_NET_ADMIN", denied.Message, StringComparison.OrdinalIgnoreCase);
            return;
        }

        var change = new NetworkRouteChange
        {
            Destination = "192.0.2.88",
            PrefixLength = 32,
            Gateway = "192.0.2.1",
            InterfaceIndex = 1
        };

        try
        {
            NetworkHelper.AddRoute(change);
            try { NetworkHelper.RemoveRoute(change); } catch { }
        }
        catch (NetworkRouteDenied ex)
        {
            Assert.DoesNotContain("not in v1", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void PR04_004_still_no_ipv6_write()
    {
        Assert.Throws<ArgumentException>(() => NetworkHelper.AddRoute(new NetworkRouteChange
        {
            Destination = "2001:db8::",
            PrefixLength = 32,
            Gateway = "2001:db8::1",
            InterfaceIndex = 1
        }));
    }
}
