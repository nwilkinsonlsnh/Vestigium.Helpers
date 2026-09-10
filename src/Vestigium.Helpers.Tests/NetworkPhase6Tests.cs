using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPhase6Tests
{
    [Fact]
    public void GetSnapshot_is_populated()
    {
        var snapshot = NetworkHelper.GetSnapshot();
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.Workstation);
        Assert.NotNull(snapshot.Routes);
        Assert.NotNull(snapshot.Connections);
        Assert.NotNull(snapshot.Neighbors);
        Assert.NotNull(snapshot.Statistics);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.Workstation.HostName));
    }

    [Fact]
    public void AddRoute_linux_is_typed_deny()
    {
        if (!OperatingSystem.IsLinux())
            return;
        Assert.Throws<PlatformNotSupportedException>(() =>
            NetworkHelper.AddRoute(new NetworkRouteChange
            {
                Destination = "192.0.2.0",
                PrefixLength = 32,
                Gateway = "127.0.0.1"
            }));
    }

    [Fact]
    public void AddRoute_windows_testnet_succeeds_or_is_denied()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var gateway = NetworkHelper.GetRoutes(RouteFamily.IPv4)
            .FirstOrDefault(r => r.Destination is "0.0.0.0" or "0.0.0.0/0")?.Gateway
            ?? "127.0.0.1";

        var change = new NetworkRouteChange
        {
            Destination = "192.0.2.0",
            PrefixLength = 32,
            Gateway = gateway,
            Metric = 9999,
            Persistent = false
        };

        try
        {
            NetworkHelper.AddRoute(change);
            NetworkHelper.RemoveRoute(change);
        }
        catch (NetworkRouteDenied)
        {
        }
    }

    [Fact]
    public void GetNetBios_windows_or_typed_deny()
    {
        if (OperatingSystem.IsWindows())
        {
            var info = NetworkHelper.GetNetBios();
            Assert.False(string.IsNullOrWhiteSpace(info.HostName));
        }
        else
        {
            Assert.Throws<PlatformNotSupportedException>(NetworkHelper.GetNetBios);
        }
    }
}
