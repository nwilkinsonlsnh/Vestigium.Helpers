using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR02Tests
{
    static NetworkRouteChange TestNet()
        => new()
        {
            Destination = "192.0.2.0",
            PrefixLength = 24,
            Gateway = "192.0.2.1",
            InterfaceIndex = 1
        };

    [Fact]
    public void PR02_002_linux_add_route_is_network_route_denied()
    {
        if (OperatingSystem.IsWindows())
            return;

        var change = TestNet();
        var ex = Assert.Throws<NetworkRouteDenied>(() => NetworkHelper.AddRoute(change));
        Assert.Contains("Linux", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not in v1", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<NetworkRouteDenied>(() => NetworkHelper.ChangeRoute(change));
        Assert.Throws<NetworkRouteDenied>(() => NetworkHelper.RemoveRoute(change));
        Assert.Throws<NetworkRouteDenied>(() => NetworkHelper.DeleteRoute(change));
    }

    [Fact]
    public void PR02_002_windows_add_route_without_admin_is_denied()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var change = new NetworkRouteChange
        {
            Destination = "192.0.2.250",
            PrefixLength = 32,
            Gateway = "192.0.2.1",
            InterfaceIndex = NetworkRouteMutation.TryFirstIpv4Index() ?? 1,
            Persistent = false
        };

        try
        {
            NetworkHelper.AddRoute(change);
            try { NetworkHelper.RemoveRoute(change); } catch { }
        }
        catch (NetworkRouteDenied)
        {
        }
    }
}
