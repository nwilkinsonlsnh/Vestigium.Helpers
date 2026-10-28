using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PathpingSampleTests
{
    [Fact]
    public void Settled_protocol_is_the_walk_protocol()
    {
        var walk = new IcmpTraceResult(
            "t",
            "127.0.0.1",
            "127.0.0.1",
            NetworkJobStatus.Success,
            true,
            ProbeProtocol.Tcp,
            1,
            [new IcmpTraceHop(1, "127.0.0.1", [])]);
        Assert.Equal(ProbeProtocol.Tcp, PathpingEngine.SettledProtocol(walk));
    }

    [Fact]
    public void Tcp_named_hop_counts_as_a_sample_hit()
    {
        var row = new IcmpTraceProbe(1, 1, ProbeProtocol.Tcp, IcmpEchoStatus.Success, "127.0.0.1", 2, "tcp-open");
        Assert.True(PathpingEngine.SampleHit(row));
    }
}
