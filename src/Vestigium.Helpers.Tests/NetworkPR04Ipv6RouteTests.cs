using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPr04Ipv6RouteTests
{
    private static NetworkRouteChange DocV6() => new()
    {
        Destination = "2001:db8:1::",
        PrefixLength = 64,
        Gateway = "2001:db8:1::1",
        InterfaceIndex = 1
    };

    [Fact]
    public void PR04_005_ipv6_default_is_not_offered()
    {
        var ex = Assert.Throws<NetworkRouteDenied>(() => NetworkHelper.AddRoute(new NetworkRouteChange
        {
            Destination = "::",
            PrefixLength = 0,
            Gateway = "2001:db8::1",
            InterfaceIndex = 1
        }));
        Assert.Contains("Default route", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PR04_005_family_mismatch_rejected()
    {
        var ex = Assert.Throws<ArgumentException>(() => NetworkHelper.AddRoute(new NetworkRouteChange
        {
            Destination = "2001:db8::",
            PrefixLength = 32,
            Gateway = "192.0.2.1",
            InterfaceIndex = 1
        }));
        Assert.Contains("family", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PR04_005_ipv6_write_is_not_v4_argument()
    {
        var change = DocV6();
        try
        {
            NetworkHelper.AddRoute(change);
            try { NetworkHelper.RemoveRoute(change); } catch { }
        }
        catch (NetworkRouteDenied)
        {
        }
        catch (ArgumentException ex)
        {
            Assert.DoesNotContain("must be IPv4", ex.Message, StringComparison.OrdinalIgnoreCase);
            throw;
        }
    }

    [Fact]
    public void PR04_005_ipv6_prefix_129_rejected()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.AddRoute(new NetworkRouteChange
        {
            Destination = "2001:db8::",
            PrefixLength = 129,
            Gateway = "2001:db8::1",
            InterfaceIndex = 1
        }));
        Assert.Equal("PrefixLength", ex.ParamName);
    }
}
