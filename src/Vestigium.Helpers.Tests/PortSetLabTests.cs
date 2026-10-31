using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PortSetLabTests
{
    [Theory]
    [InlineData("windows", 6)]
    [InlineData("printers", 3)]
    [InlineData("databases", 5)]
    public void Lab_sets_expand_under_the_cap(string name, int count)
    {
        var set = PortSet.WellKnown(name);
        Assert.Equal(count, set.Ports.Count);
        Assert.True(set.Ports.Count <= PortSet.MaxPorts);
    }

    [Fact]
    public void Windows_set_is_the_admin_ports()
    {
        Assert.Equal([135, 139, 445, 3389, 5985, 5986], PortSet.WellKnown("windows").Ports);
    }

    [Fact]
    public void Unknown_set_still_throws()
    {
        Assert.Throws<ArgumentException>(() => PortSet.WellKnown("lab"));
    }
}
