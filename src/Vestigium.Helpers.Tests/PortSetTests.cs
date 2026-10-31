using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PortSetTests
{
    [Fact]
    public void List_is_distinct_and_ordered()
    {
        var set = NetworkHelper.PortsFromList([443, 22, 80, 22, 443]);
        Assert.Equal([22, 80, 443], set.Ports);
    }

    [Fact]
    public void Range_is_inclusive()
    {
        var set = PortSet.FromRange(80, 82);
        Assert.Equal([80, 81, 82], set.Ports);
    }

    [Fact]
    public void Range_over_cap_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PortSet.FromRange(1, PortSet.MaxPorts + 1));
    }

    [Fact]
    public void Invalid_port_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PortSet.FromList([0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PortSet.FromList([70000]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PortSet.FromRange(10, 1));
    }

    [Fact]
    public void Well_known_common_is_small_and_catalog_is_capped()
    {
        var common = NetworkHelper.PortsWellKnown("common");
        Assert.Contains(443, common.Ports);
        Assert.Contains(3389, common.Ports);
        Assert.True(common.Ports.Count < 64);

        var catalog = PortSet.WellKnown("catalog");
        Assert.True(catalog.Ports.Count > 0);
        Assert.True(catalog.Ports.Count <= PortSet.MaxPorts);
        Assert.Throws<ArgumentException>(() => PortSet.WellKnown("nope"));
    }
}
