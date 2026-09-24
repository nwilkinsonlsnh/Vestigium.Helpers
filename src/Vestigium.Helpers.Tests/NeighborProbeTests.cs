using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NeighborProbeTests
{
    [Fact]
    public void Garbage_address_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => NetworkHelper.ProbeNeighbor("not-an-ip"));
    }

    [Fact]
    public void Loopback_returns_a_result_without_scanning()
    {
        var result = NetworkHelper.ProbeNeighbor("127.0.0.1");
        Assert.Equal("127.0.0.1", result.Address);
        Assert.True(result.Found || result.MacAddress is null);
    }
}
