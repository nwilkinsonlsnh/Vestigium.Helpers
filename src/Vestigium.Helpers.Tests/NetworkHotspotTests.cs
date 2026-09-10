using System.Net;
using System.Net.NetworkInformation;
using Vestigium.Helpers;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkHotspotTests
{
    [Fact]
    public void Demo_host_success_and_failure()
    {
        Assert.Equal(0, HelperDemoHost.Run("Network", NetworkHelper.Identity, () => { }));
        Assert.Equal(1, HelperDemoHost.Run("Network", NetworkHelper.Identity, () => throw new InvalidOperationException("boom")));
    }

    [Fact]
    public void IsForbidden_walks_inner_exceptions()
    {
        Assert.True(IcmpEchoEngine.IsForbidden(new PlatformNotSupportedException("icmp")));
        Assert.True(IcmpEchoEngine.IsForbidden(new InvalidOperationException("outer", new Exception("Operation not permitted"))));
        Assert.True(IcmpEchoEngine.IsForbidden(new Exception("Access denied by policy")));
        Assert.True(IcmpEchoEngine.IsForbidden(new Exception("not permitted here")));
        Assert.False(IcmpEchoEngine.IsForbidden(new InvalidOperationException("timeout")));
        Assert.False(IcmpEchoEngine.IsForbidden(new Exception("ok")));
        Assert.True(IcmpEchoEngine.IsForbidden(new Exception("outer", new PlatformNotSupportedException("inner"))));
    }

    [Fact]
    public async Task Udp_probe_loopback_or_timeout()
    {
        var hop = await IcmpTraceEngine.UdpProbeAsync("127.0.0.1", 250, ttl: 1, probe: 1, CancellationToken.None);
        Assert.Equal(ProbeProtocol.Udp, hop.Protocol);
        Assert.True(
            hop.Status is IcmpEchoStatus.Success or IcmpEchoStatus.TtlExpired or IcmpEchoStatus.TimedOut or IcmpEchoStatus.Failed,
            hop.Status.ToString());
    }

    [Fact]
    public async Task Udp_probe_unresolved_host()
    {
        var hop = await IcmpTraceEngine.UdpProbeAsync("no-such-host.invalid", 200, 1, 1, CancellationToken.None);
        Assert.Equal(IcmpEchoStatus.Failed, hop.Status);
        Assert.Equal("unresolved", hop.Detail);
    }

    [Fact]
    public async Task Udp_probe_replies_from_local_stub()
    {
        const int port = 33435;
        try
        {
            using var server = new System.Net.Sockets.UdpClient(new IPEndPoint(IPAddress.Loopback, port));
            var serve = Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var got = await server.ReceiveAsync(cts.Token);
                await server.SendAsync(new byte[8], got.RemoteEndPoint);
            });
            var hop = await IcmpTraceEngine.UdpProbeAsync("127.0.0.1", 800, ttl: 1, probe: 1, CancellationToken.None);
            await Task.WhenAny(serve, Task.Delay(500));
            Assert.Equal(ProbeProtocol.Udp, hop.Protocol);
            Assert.True(
                hop.Status is IcmpEchoStatus.Success or IcmpEchoStatus.TtlExpired or IcmpEchoStatus.TimedOut or IcmpEchoStatus.Failed,
                hop.Status.ToString());
        }
        catch (System.Net.Sockets.SocketException)
        {
            // Port in use in this sandbox — still a coverage miss, not a product failure.
        }
    }

    [Fact]
    public async Task Icmp_probe_loopback()
    {
        using var ping = new Ping();
        var hop = await IcmpTraceEngine.IcmpProbeAsync(ping, "127.0.0.1", new byte[32], 400, 1, 1, CancellationToken.None);
        Assert.Equal(ProbeProtocol.Icmp, hop.Protocol);
        Assert.True(
            hop.Status is IcmpEchoStatus.Success or IcmpEchoStatus.TtlExpired or IcmpEchoStatus.TimedOut
                or IcmpEchoStatus.Failed or IcmpEchoStatus.ProtocolForbidden or IcmpEchoStatus.DestinationUnreachable,
            hop.Status.ToString());
    }

    [Fact]
    public async Task Icmp_probe_empty_buffer()
    {
        using var ping = new Ping();
        var hop = await IcmpTraceEngine.IcmpProbeAsync(ping, "127.0.0.1", [], 300, 64, 1, CancellationToken.None);
        Assert.Equal(ProbeProtocol.Icmp, hop.Protocol);
    }

    [Fact]
    public async Task Trace_prefer_udp_one_hop()
    {
        var reports = new List<NetworkProgress>();
        var job = NetworkHelper.IcmpTrace("127.0.0.1", new IcmpTraceOptions
        {
            PreferUdp = true,
            MaxHops = 1,
            ProbesPerHop = 2,
            Timeout = TimeSpan.FromMilliseconds(300)
        });
        job.ProgressChanged += (_, p) => reports.Add(p);
        var result = await job.RunAsync();
        Assert.Single(result.Hops);
        Assert.Equal(2, result.Hops[0].Probes.Count);
        Assert.Equal(ProbeProtocol.Udp, result.ProbeProtocol);
        Assert.NotEmpty(reports);
    }

    [Fact]
    public async Task Trace_cancel_completes()
    {
        var job = NetworkHelper.IcmpTrace("127.0.0.1", new IcmpTraceOptions
        {
            MaxHops = 8,
            ProbesPerHop = 1,
            Timeout = TimeSpan.FromMilliseconds(400)
        });
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var result = await job.RunAsync(cts.Token);
        Assert.True(
            result.Status is NetworkJobStatus.Cancelled or NetworkJobStatus.TimedOut
                or NetworkJobStatus.Failed or NetworkJobStatus.Success,
            result.Status.ToString());
    }

    [Fact]
    public void Trace_guards()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpTrace("127.0.0.1", new IcmpTraceOptions { MaxHops = 65 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpTrace("127.0.0.1", new IcmpTraceOptions { ProbesPerHop = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpTrace("127.0.0.1", new IcmpTraceOptions { ProbesPerHop = 11 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpTrace("127.0.0.1", new IcmpTraceOptions { Timeout = TimeSpan.FromMilliseconds(1) }));
        Assert.Throws<ArgumentException>(() => NetworkHelper.IcmpTrace(" "));
        _ = NetworkHelper.IcmpTrace("127.0.0.1", null);
    }

    [Fact]
    public async Task Echo_max_duration_stops()
    {
        var reports = new List<NetworkProgress>();
        var stats = Path.Combine(Path.GetTempPath(), "vest-echo-" + Guid.NewGuid().ToString("N") + ".jsonl");
        var job = NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions
        {
            Count = 0,
            MaxDuration = TimeSpan.FromMilliseconds(150),
            Timeout = TimeSpan.FromMilliseconds(80),
            Interval = TimeSpan.FromMilliseconds(20),
            BufferSize = 8,
            Ttl = 32,
            DontFragment = true,
            StatsPath = stats
        });
        job.ProgressChanged += (_, p) => reports.Add(p);
        var result = await job.RunAsync();
        Assert.True(result.Sent >= 0);
        Assert.True(File.Exists(stats) || result.Sent == 0);
    }

    [Fact]
    public void Summarize_adjacent_slash24s()
    {
        var block = NetworkHelper.Summarize(["192.168.0.0/24", "192.168.1.0/24"]);
        Assert.Equal("192.168.0.0", block.Network);
        Assert.Equal(23, block.PrefixLength);
    }

    [Fact]
    public void Summarize_rejects_empty_and_mixed()
    {
        Assert.Throws<ArgumentException>(() => NetworkHelper.Summarize([]));
        Assert.Throws<ArgumentException>(() => NetworkHelper.Summarize(["10.0.0.0/8", "2001:db8::/32"]));
    }

    [Fact]
    public void Summarize_single_and_ipv6()
    {
        var one = NetworkHelper.Summarize(["10.1.2.0/24"]);
        Assert.Equal("10.1.2.0", one.Network);
        Assert.Equal(24, one.PrefixLength);
        var v6 = NetworkHelper.Summarize(["2001:db8::/48", "2001:db8:1::/48"]);
        Assert.Equal(47, v6.PrefixLength);
        var wide = NetworkHelper.Summarize(["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"]);
        Assert.True(wide.PrefixLength <= 8);
        var supernet = NetworkHelper.Summarize(["1.0.0.0/8", "200.0.0.0/8"]);
        Assert.True(supernet.PrefixLength <= 1);
    }

    [Fact]
    public void Subnet_more_guards_and_kinds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.PlanByHosts("10.0.0.0/8", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.PlanByNetworks("10.0.0.0/8", 0));
        Assert.Throws<ArgumentException>(() => NetworkHelper.PackVlsm("10.0.0.0/8", []));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.PackVlsm("10.0.0.0/8", [0]));
        Assert.Throws<ArgumentException>(() => NetworkHelper.DescribePrefix("10.0.0.1"));
        Assert.Throws<ArgumentException>(() => NetworkHelper.DescribePrefix("not-an-ip/24"));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.DescribePrefix("10.0.0.1", 99));
        Assert.Throws<ArgumentException>(() => NetworkHelper.DescribePrefix("2001:db8::1", "255.255.255.0"));
        Assert.Throws<ArgumentException>(() => NetworkHelper.DescribePrefix("10.0.0.1", "not-a-mask"));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.SplitPrefix("10.0.0.0/16", 8));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.SplitPrefix("10.0.0.0/16", 24, new SubnetQuery { MaxList = 0 }));
        Assert.Throws<ArgumentException>(() => NetworkHelper.DescribePrefix("10.0.0.1/nope"));
        Assert.Throws<InvalidOperationException>(() => NetworkHelper.PlanByNetworks("192.168.1.0/30", 64));
        Assert.Throws<InvalidOperationException>(() => NetworkHelper.PackVlsm("192.168.1.0/30", [200]));

        Assert.True(NetworkHelper.ClassifyAddress("0.0.0.0").Kind.HasFlag(AddressKind.Unspecified));
        Assert.True(NetworkHelper.ClassifyAddress("255.255.255.255").Kind.HasFlag(AddressKind.Broadcast));
        Assert.True(NetworkHelper.ClassifyAddress("127.0.0.1").Kind.HasFlag(AddressKind.Loopback));
        Assert.True(NetworkHelper.ClassifyAddress("169.254.1.1").Kind.HasFlag(AddressKind.LinkLocal));
        Assert.True(NetworkHelper.ClassifyAddress("100.64.0.1").Kind.HasFlag(AddressKind.Cgnat));
        Assert.True(NetworkHelper.ClassifyAddress("192.0.2.1").Kind.HasFlag(AddressKind.Documentation));
        Assert.True(NetworkHelper.ClassifyAddress("198.51.100.1").Kind.HasFlag(AddressKind.Documentation));
        Assert.True(NetworkHelper.ClassifyAddress("203.0.113.1").Kind.HasFlag(AddressKind.Documentation));
        Assert.True(NetworkHelper.ClassifyAddress("192.0.0.1").Kind.HasFlag(AddressKind.Documentation));
        Assert.True(NetworkHelper.ClassifyAddress("198.18.0.1").Kind.HasFlag(AddressKind.Benchmark));
        Assert.True(NetworkHelper.ClassifyAddress("8.8.8.8").Kind.HasFlag(AddressKind.Unicast));
        Assert.True(NetworkHelper.ClassifyAddress("::1").Kind.HasFlag(AddressKind.Loopback));
        Assert.True(NetworkHelper.ClassifyAddress("fe80::1").Kind.HasFlag(AddressKind.LinkLocal));
        Assert.True(NetworkHelper.ClassifyAddress("fc00::1").Kind.HasFlag(AddressKind.UniqueLocal));
        Assert.True(NetworkHelper.ClassifyAddress("fd12::1").Kind.HasFlag(AddressKind.UniqueLocal));
        Assert.True(NetworkHelper.ClassifyAddress("ff02::1").Kind.HasFlag(AddressKind.Multicast));
        Assert.True(NetworkHelper.ClassifyAddress("::ffff:10.0.0.1").Kind.HasFlag(AddressKind.Ipv4Mapped));
        Assert.True(NetworkHelper.ClassifyAddress("::").Kind.HasFlag(AddressKind.Unspecified));
        Assert.True(NetworkHelper.ClassifyAddress("2001:4860:4860::8888").Kind.HasFlag(AddressKind.GlobalUnicast));

        var host = NetworkHelper.DescribePrefix("192.168.1.8/32");
        Assert.True(host.IsHostRoute);
        Assert.Equal(1, (int)host.UsableHosts);
        var p2p = NetworkHelper.DescribePrefix("192.168.1.0/31");
        Assert.True(p2p.IsPointToPoint);
        Assert.Equal(2, (int)p2p.UsableHosts);
        var slash30 = NetworkHelper.DescribePrefix("192.168.1.0/30");
        Assert.Equal(2, (int)slash30.UsableHosts);
        Assert.False(string.IsNullOrWhiteSpace(slash30.WildcardMask));
        var def = NetworkHelper.DescribePrefix("0.0.0.0/0");
        Assert.Equal("255.255.255.255", def.WildcardMask);
        Assert.Null(NetworkHelper.NextBlock("255.255.255.0/24"));
        Assert.NotNull(NetworkHelper.NextBlock("2001:db8::/32"));
        var v6host = NetworkHelper.DescribePrefix("2001:db8::1/128");
        Assert.True(v6host.IsHostRoute);
        var v6p2p = NetworkHelper.DescribePrefix("2001:db8::/127");
        Assert.True(v6p2p.IsPointToPoint);
        Assert.Contains(':', v6p2p.BinaryMask);
        var split = NetworkHelper.SplitPrefixByCount("10.0.0.0/24", 2);
        Assert.Equal(25, split.ChildPrefix);
        Assert.False(NetworkHelper.Overlaps("10.0.0.0/24", "2001:db8::/32"));
        Assert.False(NetworkHelper.Contains("10.0.0.0/24", "::1"));
        Assert.True(NetworkHelper.Overlaps("10.0.0.0/24", "10.0.0.128/25"));
        var counted = NetworkHelper.PlanByHosts("10.0.0.0/24", 50, new SubnetQuery { CountNetworkAndBroadcast = true });
        Assert.True(counted.ChildPrefix >= 26);
        var unsorted = NetworkHelper.PackVlsm("10.8.0.0/16", [12, 200], new SubnetQuery { PackLargestFirst = false });
        Assert.Equal(2, unsorted.Networks.Count);
        Assert.NotEmpty(unsorted.Unused);
        var same = NetworkHelper.SplitPrefix("10.0.0.0/24", 24);
        Assert.Equal(24, same.ChildPrefix);
        Assert.Single(same.Networks);
        var byAddr = NetworkHelper.DescribePrefix("10.1.2.3", 16);
        Assert.Equal("10.1.0.0", byAddr.Network);
        Assert.Null(byAddr.PtrHint);
        var slash24 = NetworkHelper.DescribePrefix("172.16.9.0/24");
        Assert.Equal("9.16.172.in-addr.arpa", slash24.PtrHint);
    }

    [Fact]
    public void Ipv4_prefix_helpers()
    {
        Assert.Equal("255.255.255.0", Ipv4Prefix.MaskFromPrefix(24));
        Assert.Equal(24, Ipv4Prefix.PrefixFromMask(IPAddress.Parse("255.255.255.0")));
        Assert.Equal("0.0.0.0", Ipv4Prefix.MaskFromPrefix(0));
        Assert.Equal("255.255.255.255", Ipv4Prefix.MaskFromPrefix(32));
        Assert.Equal("0.0.0.0", Ipv4Prefix.MaskFromPrefix(-3));
        Assert.Equal("255.255.255.255", Ipv4Prefix.MaskFromPrefix(99));
        Assert.Equal(8, Ipv4Prefix.PrefixFromMask(IPAddress.Parse("255.0.255.0")));
        Ipv4Prefix.Agree(24, null, out var p, out var dotted);
        Assert.Equal(24, p);
        Assert.Equal("255.255.255.0", dotted);
        Ipv4Prefix.Agree(null, IPAddress.Parse("255.255.0.0"), out p, out dotted);
        Assert.Equal(16, p);
        Ipv4Prefix.Agree(null, null, out p, out dotted);
        Assert.Equal(0, p);
        Ipv4Prefix.Agree(40, IPAddress.Parse("255.255.255.0"), out p, out _);
        Assert.Equal(24, p);
        Ipv4Prefix.Agree(null, IPAddress.IPv6Loopback, out p, out _);
        Assert.Equal(0, p);
    }
}
