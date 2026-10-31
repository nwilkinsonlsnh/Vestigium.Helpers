using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class UdpPayloadTests
{
    [Fact]
    public void Known_udp_is_off_by_default()
    {
        Assert.False(new PortScanOptions().KnownUdp);
        Assert.Null(new UdpProbeOptions().Payload);
    }

    [Fact]
    public void Dns_and_ntp_have_one_datagram_and_other_ports_do_not()
    {
        Assert.Equal(17, UdpPayloads.For(53)!.Length);
        Assert.Equal(48, UdpPayloads.For(123)!.Length);
        Assert.Equal(0x1B, UdpPayloads.For(123)![0]);
        Assert.Null(UdpPayloads.For(161));
        Assert.Null(UdpPayloads.For(9));
    }
}
