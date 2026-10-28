using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPortCatalogTests
{
    [Fact]
    public void Udp_443_is_quic_and_tcp_443_stays_https()
    {
        Assert.True(NetworkPorts.Try("UDP", 443, out var udp));
        Assert.Equal("quic", udp.Name);
        Assert.True(NetworkPorts.Try("TCP", 443, out var tcp));
        Assert.Equal("https", tcp.Name);
        Assert.True(NetworkPorts.TryByPort(443, out var first));
        Assert.Equal("https", first.Name);
    }

    [Fact]
    public void Tcp_514_is_rsh_and_udp_514_is_syslog()
    {
        Assert.True(NetworkPorts.Try("TCP", 514, out var tcp));
        Assert.Equal("rsh", tcp.Name);
        Assert.True(NetworkPorts.Try("UDP", 514, out var udp));
        Assert.Equal("syslog", udp.Name);
    }

    [Fact]
    public void TryService_blank_protocol_is_tcp_and_a_miss_is_false()
    {
        Assert.True(NetworkHelper.TryService(null, 22, out var name));
        Assert.Equal("ssh", name);
        Assert.True(NetworkHelper.TryService("  ", 443, out name));
        Assert.Equal("https", name);
        Assert.False(NetworkHelper.TryService("TCP", 4, out name));
        Assert.Equal(string.Empty, name);
        Assert.False(NetworkPorts.TryByPort(860, out _));
        Assert.True(NetworkPorts.TryByPort(3260, out var iscsi));
        Assert.Equal("iscsi", iscsi.Name);
    }
}
