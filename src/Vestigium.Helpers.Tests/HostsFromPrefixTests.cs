using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class HostsFromPrefixTests
{
    [Fact]
    public void Slash_30_returns_the_two_usable_hosts()
    {
        var hosts = NetworkHelper.HostsFromPrefix("192.0.2.0/30");
        Assert.Equal(["192.0.2.1", "192.0.2.2"], hosts);
    }

    [Fact]
    public void Slash_32_returns_the_one_address()
    {
        Assert.Equal(["192.0.2.1"], NetworkHelper.HostsFromPrefix("192.0.2.1/32"));
    }

    [Fact]
    public void Over_the_cap_throws_and_does_not_scan()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.HostsFromPrefix("10.0.0.0/21"));
    }
}
