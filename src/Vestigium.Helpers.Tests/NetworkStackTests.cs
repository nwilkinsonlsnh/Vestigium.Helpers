using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkStackTests
{
    [Fact]
    public void GetConnections_returns_list()
    {
        var rows = NetworkHelper.GetConnections();
        Assert.NotNull(rows);
    }

    [Fact]
    public void GetStatistics_does_not_throw()
    {
        var stats = NetworkHelper.GetStatistics();
        Assert.NotNull(stats);
    }

    [Fact]
    public void GetRoutes_returns_list()
    {
        var rows = NetworkHelper.GetRoutes();
        Assert.NotNull(rows);
        foreach (var row in rows.Where(r => r.Family == System.Net.Sockets.AddressFamily.InterNetwork))
        {
            Assert.InRange(row.PrefixLength, 0, 32);
            if (row.Mask is not null)
                Assert.Equal(Ipv4Prefix.MaskFromPrefix(row.PrefixLength), row.Mask);
        }
    }

    [Fact]
    public void GetNeighbors_returns_list()
    {
        var rows = NetworkHelper.GetNeighbors();
        Assert.NotNull(rows);
    }

    [Fact]
    public void ListeningOnly_filters_listen_rows()
    {
        var rows = NetworkHelper.GetConnections(new NetworkConnectionQuery(ListeningOnly: true));
        Assert.All(rows, r => Assert.Equal("Listen", r.State, StringComparer.OrdinalIgnoreCase));
    }
}
