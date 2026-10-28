using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class UdpProbeTests
{
    [Fact]
    public void Kind_is_udpProbe()
    {
        var job = NetworkHelper.UdpProbe("127.0.0.1", 9, new UdpProbeOptions { Timeout = TimeSpan.FromMilliseconds(200) });
        Assert.Equal("udpProbe", job.Kind);
    }

    [Fact]
    public void Port_out_of_range_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.UdpProbe("127.0.0.1", 0));
    }

    [Fact]
    public async Task Loopback_discard_returns_a_status()
    {
        var job = NetworkHelper.UdpProbe("127.0.0.1", 9, new UdpProbeOptions { Timeout = TimeSpan.FromMilliseconds(300) });
        var result = await job.RunAsync();
        Assert.Equal(9, result.Port);
        Assert.Contains(result.Status, new[] { UdpProbeStatus.Replied, UdpProbeStatus.Unreachable, UdpProbeStatus.TimedOut });
        Assert.True(result.ElapsedMs >= 0);
    }
}
