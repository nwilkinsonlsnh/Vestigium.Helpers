using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class TcpMssTests
{
    [Fact]
    public void Connected_result_carries_nullable_mss()
    {
        var result = new TcpConnectResult("tcp-test", "127.0.0.1", 80, "127.0.0.1", TcpConnectStatus.Connected, 1, "connected", 1460);
        Assert.Equal(1460, result.Mss);
        Assert.Equal(TcpConnectStatus.Connected, result.Status);
    }

    [Fact]
    public void Miss_stays_null_and_does_not_change_status()
    {
        var refused = new TcpConnectResult("tcp-test", "127.0.0.1", 1, "127.0.0.1", TcpConnectStatus.Refused, 1, "ConnectionRefused");
        var timedOut = new TcpConnectResult("tcp-test", "127.0.0.1", 1, "127.0.0.1", TcpConnectStatus.TimedOut, 1, "timeout");
        var connected = new TcpConnectResult("tcp-test", "127.0.0.1", 80, "127.0.0.1", TcpConnectStatus.Connected, 1, "connected");

        Assert.Null(refused.Mss);
        Assert.Equal(TcpConnectStatus.Refused, refused.Status);
        Assert.Null(timedOut.Mss);
        Assert.Equal(TcpConnectStatus.TimedOut, timedOut.Status);
        Assert.Null(connected.Mss);
        Assert.Equal(TcpConnectStatus.Connected, connected.Status);
    }
}
