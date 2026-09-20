using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR02RouteFamilyTests
{
    [Fact]
    public void PR02_006_ipv6_destination_rejected_on_add()
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
        Assert.Throws<ArgumentException>(() => NetworkHelper.DeleteRoute(change));
    }

    [Fact]
    public void PR02_006_ipv6_prefix_length_rejected_on_add()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.AddRoute(new NetworkRouteChange
            {
                Destination = "192.0.2.0",
                PrefixLength = 128,
                Gateway = "192.0.2.1",
                InterfaceIndex = 1
            }));
        Assert.Equal("PrefixLength", ex.ParamName);
    }

    [Fact]
    public void PR02_006_ipv6_print_does_not_throw()
    {
        var rows = NetworkHelper.GetRoutes(RouteFamily.IPv6);
        Assert.NotNull(rows);
    }
}
