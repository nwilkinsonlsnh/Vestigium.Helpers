using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class TcpBannerTests
{
    [Fact]
    public void Banner_read_is_off_by_default()
    {
        var options = new TcpConnectOptions();
        Assert.False(options.ReadBanner);
        Assert.Equal(TcpConnectOptions.DefaultBannerMaxBytes, options.BannerMaxBytes);
    }

    [Fact]
    public void Banner_is_optional_and_does_not_change_status()
    {
        var off = new TcpConnectResult("tcp-test", "127.0.0.1", 22, "127.0.0.1", TcpConnectStatus.Connected, 1, "connected", 1460);
        var on = new TcpConnectResult("tcp-test", "127.0.0.1", 22, "127.0.0.1", TcpConnectStatus.Connected, 1, "connected", 1460, "SSH-2.0-OpenSSH_9.6");

        Assert.Null(off.Banner);
        Assert.Equal(TcpConnectStatus.Connected, off.Status);
        Assert.Equal("SSH-2.0-OpenSSH_9.6", on.Banner);
        Assert.Equal(TcpConnectStatus.Connected, on.Status);
    }
}
