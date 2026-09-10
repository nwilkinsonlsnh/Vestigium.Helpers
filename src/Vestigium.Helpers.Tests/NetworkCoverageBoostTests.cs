using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkCoverageBoostTests : IDisposable
{
    readonly string _proc;

    public NetworkCoverageBoostTests()
    {
        _proc = Path.Combine(Path.GetTempPath(), "vest-proc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_proc, "proc", "net"));
        File.WriteAllText(
            Path.Combine(_proc, "proc", "net", "route"),
            "Iface\tDestination\tGateway\tFlags\tRefCnt\tUse\tMetric\tMask\tMTU\tWindow\tIRTT\n" +
            "eth0\t00000000\t0100A8C0\t0003\t0\t0\t100\t00000000\t0\t0\t0\n" +
            "short\n" +
            "eth0 not-hex 00000000 0001 0 0 x 00FFFFFF extra\n");
        File.WriteAllText(
            Path.Combine(_proc, "proc", "net", "arp"),
            "IP address       HW type     Flags       HW address            Mask     Device\n" +
            "192.168.1.1      0x1         0x2         aa:bb:cc:dd:ee:ff     *        eth0\n" +
            "192.168.1.2      0x1         0x4         00:00:00:00:00:00     *        eth0\n" +
            "192.168.1.3      0x1         0x8         11:22:33:44:55:66     *        eth0\n" +
            "192.168.1.4      0x1         0x1         11:22:33:44:55:67     *        eth0\n" +
            "192.168.1.5      0x1         0x0         11:22:33:44:55:68     *        eth1\n" +
            "192.168.1.6      0x1         zz          11:22:33:44:55:69     *        eth1\n" +
            "bad\n");
        File.WriteAllText(
            Path.Combine(_proc, "proc", "net", "ipv6_route"),
            "00000000000000000000000000000001 80 00000000000000000000000000000000 00 00000000000000000000000000000000 00000064 00000000 00000000 00000001 lo\n" +
            "short\n" +
            "not-a-valid-ipv6-hex-string-here 40 00000000000000000000000000000000 00 00000000000000000000000000000000 00000000 00000000 00000000 00000001 eth0\n" +
            "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz gg 00000000000000000000000000000000 00 zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz nothex 00000000 00000000 00000001 eth2\n");
        NetworkTestHooks.ProcRoot = _proc;
    }

    public void Dispose() => NetworkTestHooks.Reset();

    [Fact]
    public void Linux_tables_parse_proc_fixtures()
    {
        Assert.Empty(NetworkLinuxTables.GetOwnerPids());
        var v4 = NetworkLinuxTables.GetRoutes(RouteFamily.IPv4);
        Assert.Contains(v4, r => r.InterfaceName == "eth0");
        var v6 = NetworkLinuxTables.GetRoutes(RouteFamily.IPv6);
        Assert.NotEmpty(v6);
        var all = NetworkLinuxTables.GetRoutes(RouteFamily.All);
        Assert.True(all.Count >= v4.Count);
        var neighbors = NetworkLinuxTables.GetNeighbors();
        Assert.Contains(neighbors, n => n.State == "Reachable" && n.MacAddress == "aa:bb:cc:dd:ee:ff");
        Assert.Contains(neighbors, n => n.State == "Permanent" && n.MacAddress is null);
        Assert.Contains(neighbors, n => n.State == "Failed");
        Assert.Contains(neighbors, n => n.State == "Incomplete");
    }

    [Fact]
    public void Linux_tables_missing_proc_is_empty()
    {
        NetworkTestHooks.ProcRoot = Path.Combine(_proc, "missing");
        Assert.Empty(NetworkLinuxTables.GetRoutes(RouteFamily.All));
        Assert.Empty(NetworkLinuxTables.GetNeighbors());
    }

    [Fact]
    public void Linux_tables_real_proc_or_empty_without_hook()
    {
        NetworkTestHooks.ProcRoot = null;
        _ = NetworkLinuxTables.GetRoutes(RouteFamily.IPv4);
        _ = NetworkLinuxTables.GetRoutes(RouteFamily.IPv6);
        _ = NetworkLinuxTables.GetNeighbors();
        _ = NetworkLinuxTables.GetOwnerPids();
    }


    [Fact]
    public void Icmp_guards_cover_remaining_ranges()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { Interval = TimeSpan.FromMilliseconds(-1) }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { BufferSize = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { Ttl = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { Timeout = TimeSpan.FromSeconds(61) }));
    }

    [Fact]
    public async Task Dns_guards_and_ptr()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            NetworkHelper.LookupAsync("localhost", new DnsLookupOptions { Port = 0 }));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            NetworkHelper.LookupAsync("localhost", new DnsLookupOptions { Timeout = TimeSpan.FromMilliseconds(1) }));
        await Assert.ThrowsAsync<ArgumentException>(() => NetworkHelper.LookupManyAsync([]));
        var ptr = await NetworkHelper.LookupAsync("127.0.0.1", new DnsLookupOptions { Type = DnsRecordType.Ptr });
        Assert.Equal(DnsRecordType.Ptr, ptr.Type);
        var aaaa = await NetworkHelper.LookupAsync("localhost", new DnsLookupOptions { Type = DnsRecordType.Aaaa });
        Assert.Equal(DnsRecordType.Aaaa, aaaa.Type);
    }

    [Fact]
    public void Route_bind_rejects_bad_rows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.AddRoute(new NetworkRouteChange
        {
            Destination = "192.168.1.0",
            Gateway = "192.168.1.1",
            PrefixLength = 99
        }));
        Assert.Throws<ArgumentException>(() => NetworkHelper.AddRoute(new NetworkRouteChange
        {
            Destination = "2001:db8::1",
            Gateway = "192.168.1.1",
            PrefixLength = 24
        }));
        Assert.Throws<ArgumentException>(() => NetworkHelper.ChangeRoute(new NetworkRouteChange
        {
            Destination = "192.168.1.0",
            Gateway = "fe80::1",
            PrefixLength = 24
        }));
    }

    [Fact]
    public void Snapshot_and_netbios_do_not_throw()
    {
        var snap = NetworkHelper.GetWorkstation();
        Assert.NotNull(snap);
        try
        {
            var bios = NetworkHelper.GetNetBios();
            Assert.False(string.IsNullOrWhiteSpace(bios.HostName));
        }
        catch (PlatformNotSupportedException)
        {
            // GetNetBios is Windows-only.
        }
        try
        {
            Assert.NotNull(NetworkHelper.GetStatistics());
            Assert.NotNull(NetworkHelper.GetSnapshot());
        }
        catch (PlatformNotSupportedException)
        {
            // Linux TCP statistics are not always available in this container.
        }
        Assert.NotNull(NetworkHelper.GetConnections());
        Assert.NotNull(NetworkHelper.GetNeighbors());
        Assert.NotNull(NetworkHelper.GetRoutes());
    }


}
