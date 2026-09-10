using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkMacTests
{
    [Fact]
    public void Parse_cisco_to_colon()
    {
        var mac = NetworkHelper.ParseMac("001A.2B3C.4D5E");
        Assert.Equal(EuiKind.Eui48, mac.Kind);
        Assert.Equal("00:1A:2B:3C:4D:5E", mac.Colon);
        Assert.Equal("001A.2B3C.4D5E", mac.Cisco);
        Assert.False(mac.IsMulticast);
        Assert.False(mac.IsBroadcast);
    }

    [Fact]
    public void Integer_round_trip()
    {
        var mac = NetworkHelper.ParseMac("00:1A:2B:3C:4D:5E");
        var again = NetworkHelper.MacFromInteger(mac.Integer);
        Assert.Equal(mac.Colon, again.Colon);
        Assert.Equal(mac.Integer, again.Integer);
        Assert.Equal(mac.Colon, NetworkHelper.FormatMac(mac, MacFormat.Colon));
    }

    [Fact]
    public void Broadcast_and_multicast_flags()
    {
        var bcast = NetworkHelper.ParseMac("ff-ff-ff-ff-ff-ff");
        Assert.True(bcast.IsBroadcast);
        Assert.True(bcast.IsMulticast);

        var mcast = NetworkHelper.ParseMac("01:00:5E:00:00:01");
        Assert.True(mcast.IsMulticast);
        Assert.False(mcast.IsBroadcast);
        Assert.False(mcast.IsUnspecified);
    }

    [Fact]
    public void Modified_eui64_inserts_fffe_and_flips_ul()
    {
        var mac = NetworkHelper.ParseMac("00:1A:2B:3C:4D:5E");
        var eui64 = NetworkHelper.ToModifiedEui64(mac);
        Assert.Equal(EuiKind.Eui64, eui64.Kind);
        Assert.Equal("02:1A:2B:FF:FE:3C:4D:5E", eui64.Colon);
        Assert.Equal((byte)(mac.Octets[0] ^ 0x02), eui64.Octets[0]);
        Assert.Equal(0xFF, eui64.Octets[3]);
        Assert.Equal(0xFE, eui64.Octets[4]);
        Assert.StartsWith("fe80:", eui64.LinkLocal, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("fe80:", NetworkHelper.ToLinkLocal(mac), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(mac.Colon, NetworkHelper.ToEui48(eui64).Colon);
    }

    [Fact]
    public void Eui64_without_fffe_cannot_collapse()
    {
        var mac = NetworkHelper.ParseMac("02:1A:2B:00:00:3C:4D:5E");
        Assert.Throws<InvalidOperationException>(() => NetworkHelper.ToEui48(mac));
    }

    [Fact]
    public async Task LookupOui_dead_endpoint_does_not_hang()
    {
        var result = await NetworkHelper.LookupOuiAsync(
            "00:1A:2B:3C:4D:5E",
            new OuiLookupOptions
            {
                Timeout = TimeSpan.FromMilliseconds(250),
                RegistryUrl = "http://127.0.0.1:1/{oui}"
            });
        Assert.Null(result.Vendor);
        Assert.Equal(OuiSource.None, result.Source);
        Assert.Contains("Not proof", result.Disclaimer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseMac_does_not_require_http()
    {
        var mac = NetworkHelper.ParseMac("00-1A-2B-3C-4D-5E");
        Assert.Equal("00:1A:2B:3C:4D:5E", mac.Colon);
        Assert.Equal("00:1A:2B", mac.Oui24);
    }
}
