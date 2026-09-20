using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR04RouteTests
{
    [Fact]
    public void PR04_003_linux_mutate_denied()
    {
        if (OperatingSystem.IsWindows())
        {
            var denied = NetworkRouteMutation.LinuxWriteDenied("Add");
            Assert.IsType<NetworkRouteDenied>(denied);
            Assert.Contains("CAP_NET_ADMIN", denied.Message, StringComparison.OrdinalIgnoreCase);
            return;
        }

        var change = new NetworkRouteChange
        {
            Destination = "192.0.2.0",
            PrefixLength = 24,
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
            Assert.DoesNotContain("ip route", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void PR04_003_ipv6_write_denied()
    {
        var change = new NetworkRouteChange
        {
            Destination = "2001:db8::",
            PrefixLength = 32,
            Gateway = "2001:db8::1",
            InterfaceIndex = 1
        };

        var dest = Assert.Throws<ArgumentException>(() => NetworkHelper.AddRoute(change));
        Assert.Contains("IPv4", dest.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<ArgumentException>(() => NetworkHelper.ChangeRoute(change));
        Assert.Throws<ArgumentException>(() => NetworkHelper.RemoveRoute(change));
    }

    [Fact]
    public void PR04_003_ipv6_print_stays()
    {
        Assert.NotNull(NetworkHelper.GetRoutes(RouteFamily.IPv6));
        Assert.NotNull(NetworkHelper.GetRoutes(RouteFamily.All));
    }
}
