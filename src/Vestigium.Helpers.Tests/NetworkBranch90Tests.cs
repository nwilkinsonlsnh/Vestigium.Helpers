using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class NetworkBranch90Tests
{
    [Fact]
    public void Public_inventory_and_helpers()
    {
        Assert.Equal(NetworkHelper.Identity, NetworkHelper.Probe());
        Assert.NotNull(NetworkHelper.GetWorkstation());
        Assert.NotNull(NetworkHelper.GetAdapters());
        Assert.NotNull(NetworkHelper.GetRoutes());
        Assert.NotNull(NetworkHelper.GetRoutes(RouteFamily.All));
        Assert.NotNull(NetworkHelper.GetConnections());
        Assert.NotNull(NetworkHelper.GetStatistics());
        Assert.NotNull(NetworkHelper.GetNeighbors());
        Assert.NotNull(NetworkHelper.GetNetBios());
        Assert.NotNull(NetworkHelper.GetSnapshot());
        Assert.NotEmpty(NetworkHelper.CommonBots);
        _ = NetworkHelper.ClassifyAddress("127.0.0.1");
        _ = NetworkHelper.ClassifyAddress("::1");
        _ = NetworkHelper.DescribePrefix("192.168.0.0/24");
        _ = NetworkHelper.Contains("192.168.0.0/24", "192.168.0.10");
        _ = NetworkHelper.Overlaps("192.168.0.0/24", "192.168.0.0/25");
        var mac = NetworkHelper.ParseMac("00:11:22:33:44:55");
        _ = NetworkHelper.FormatMac(mac);
        _ = NetworkHelper.ToModifiedEui64(mac);
        _ = NetworkHelper.MacFromInteger(1);
        _ = NetworkHelper.Bandwidth(1, DataUnit.Mb);
        _ = NetworkHelper.BandwidthSeconds(BandwidthBasis.Days30);
        _ = NetworkHelper.BillP95([1m, 2m, 3m, 4m, 5m]);
    }
}
