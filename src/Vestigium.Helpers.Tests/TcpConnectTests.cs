using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class TcpConnectTests
{
    [Fact]
    public void Kind_is_tcpConnect()
    {
        var job = NetworkHelper.TcpConnect("127.0.0.1", 1, new TcpConnectOptions { Timeout = TimeSpan.FromMilliseconds(200) });
        Assert.Equal("tcpConnect", job.Kind);
    }

    [Fact]
    public void Port_out_of_range_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.TcpConnect("127.0.0.1", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.TcpConnect("127.0.0.1", 70000));
    }

    [Fact]
    public async Task Loopback_closed_port_is_refused_or_timed_out()
    {
        var job = NetworkHelper.TcpConnect("127.0.0.1", 1, new TcpConnectOptions { Timeout = TimeSpan.FromMilliseconds(400) });
        var result = await job.RunAsync();
        Assert.Equal(1, result.Port);
        Assert.Contains(result.Status, new[] { TcpConnectStatus.Refused, TcpConnectStatus.TimedOut, TcpConnectStatus.Connected });
        Assert.True(result.ElapsedMs >= 0);
    }
}
