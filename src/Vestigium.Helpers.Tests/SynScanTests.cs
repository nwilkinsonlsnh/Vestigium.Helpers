using Vestigium.Helpers.Network;
using Vestigium.Helpers.Network.RawSocket;

namespace Vestigium.Helpers.Tests;

public sealed class SynScanTests
{
    [Fact]
    public void Kind_is_synScan()
    {
        var job = RawSocketHelper.SynScan("127.0.0.1", PortSet.FromList([1]));
        Assert.Equal("synScan", job.Kind);
    }

    [Fact]
    public void Empty_hosts_or_too_many_throw()
    {
        Assert.Throws<ArgumentException>(() => RawSocketHelper.SynScan(Array.Empty<string>(), PortSet.FromList([80])));
        var many = Enumerable.Range(1, SynScanOptions.MaxHosts + 1).Select(i => $"10.0.0.{i}");
        Assert.Throws<ArgumentOutOfRangeException>(() => RawSocketHelper.SynScan(many, PortSet.FromList([80])));
    }

    [Fact]
    public void Syn_flag_is_set_and_checksum_is_filled()
    {
        var packet = SynPacket.Build(System.Net.IPAddress.Parse("192.0.2.1"), System.Net.IPAddress.Parse("192.0.2.2"), 40000, 80, 1, 0x02);
        Assert.Equal(20, packet.Length);
        Assert.Equal(0x02, packet[13]);
        Assert.NotEqual(0, packet[16] | packet[17]);
    }

    [Fact]
    public async Task Missing_rights_is_a_typed_denial()
    {
        var access = RawSocketRights.Check();
        if (access.IsGranted)
            return;

        var job = RawSocketHelper.SynScan("192.0.2.1", PortSet.FromList([80]), new SynScanOptions { Timeout = TimeSpan.FromMilliseconds(200) });
        var result = await job.RunAsync();
        Assert.Equal(SynScanStatus.Denied, result.Status);
        Assert.Empty(result.Hits);
    }
}
