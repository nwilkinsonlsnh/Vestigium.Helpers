using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class TcpProbeTests
{
    [Fact]
    public void Tcp_port_out_of_range_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpTrace("127.0.0.1", new IcmpTraceOptions { TcpPort = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpTrace("127.0.0.1", new IcmpTraceOptions { TcpPort = 70000 }));
    }

    [Fact]
    public void Silent_udp_is_timeout_without_address()
    {
        var silent = new IcmpTraceProbe(1, 1, ProbeProtocol.Udp, IcmpEchoStatus.TimedOut, null, 100, "timeout");
        Assert.True(IcmpTraceEngine.IsSilent(silent));
        var named = new IcmpTraceProbe(1, 1, ProbeProtocol.Udp, IcmpEchoStatus.TtlExpired, "10.0.0.1", 4, "udp-hop");
        Assert.False(IcmpTraceEngine.IsSilent(named));
    }

    [Fact]
    public async Task Refused_tcp_still_names_loopback()
    {
        var row = await IcmpTraceEngine.TcpProbeAsync(
            "127.0.0.1",
            timeoutMs: 400,
            ttl: 64,
            probe: 1,
            token: CancellationToken.None,
            family: RouteFamily.Pv4,
            port: 1,
            interfaceIndex: 0,
            sourceAddress: null);
        Assert.Equal(ProbeProtocol.Tcp, row.Protocol);
        if (row.Status is IcmpEchoStatus.Success or IcmpEchoStatus.DestinationUnreachable)
            Assert.Equal("127.0.0.1", row.Address);
    }
}
