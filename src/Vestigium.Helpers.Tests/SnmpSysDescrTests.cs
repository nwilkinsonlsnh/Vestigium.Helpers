using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class SnmpSysDescrTests
{
    [Fact]
    public void Kind_is_snmpSysDescr()
    {
        var job = NetworkHelper.ProbeSnmpSysDescr("127.0.0.1", new SnmpSysDescrOptions { Timeout = TimeSpan.FromMilliseconds(200) });
        Assert.Equal("snmpSysDescr", job.Kind);
    }

    [Fact]
    public void Missing_community_throws()
    {
        Assert.Throws<ArgumentException>(() => NetworkHelper.ProbeSnmpSysDescr("127.0.0.1", new SnmpSysDescrOptions { Community = " " }));
    }

    [Fact]
    public void Packet_encodes_sysDescr_oid_and_parses_octet_string()
    {
        var request = SnmpSysDescrPacket.GetRequest("public", 7);
        Assert.Contains((byte)0x06, request);
        var response = new byte[]
        {
            0x30, 0x1A,
            0x02, 0x01, 0x01,
            0x04, 0x06, 0x70, 0x75, 0x62, 0x6C, 0x69, 0x63,
            0xA2, 0x0D,
            0x02, 0x01, 0x07,
            0x02, 0x01, 0x00,
            0x02, 0x01, 0x00,
            0x30, 0x0A,
            0x30, 0x08,
            0x06, 0x08, 0x2B, 0x06, 0x01, 0x02, 0x01, 0x01, 0x01, 0x00,
            0x04, 0x05, 0x43, 0x69, 0x73, 0x63, 0x6F
        };
        Assert.Equal("Cisco", SnmpSysDescrPacket.ParseSysDescr(response));
    }

    [Fact]
    public async Task Silent_host_is_a_typed_miss()
    {
        var job = NetworkHelper.ProbeSnmpSysDescr("192.0.2.1", new SnmpSysDescrOptions { Timeout = TimeSpan.FromMilliseconds(200), Port = 161 });
        var result = await job.RunAsync();
        Assert.True(result.Status is SnmpSysDescrStatus.TimedOut or SnmpSysDescrStatus.Error);
        Assert.NotEqual(SnmpSysDescrStatus.Answered, result.Status);
        Assert.Null(result.SysDescr);
    }
}
