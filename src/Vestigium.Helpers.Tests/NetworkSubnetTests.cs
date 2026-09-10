using System.Net;
using System.Net.Sockets;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkSubnetTests
{
    [Fact]
    public void Classify_private_and_classful()
    {
        var a = NetworkHelper.ClassifyAddress("10.1.2.3");
        Assert.Equal(TraditionalClass.A, a.TraditionalClass);
        Assert.True(a.Kind.HasFlag(AddressKind.Rfc1918));

        var b = NetworkHelper.ClassifyAddress("172.16.0.1");
        Assert.Equal(TraditionalClass.B, b.TraditionalClass);
        Assert.True(b.Kind.HasFlag(AddressKind.Rfc1918));

        var c = NetworkHelper.ClassifyAddress("192.168.1.1");
        Assert.Equal(TraditionalClass.C, c.TraditionalClass);
        Assert.True(c.Kind.HasFlag(AddressKind.Rfc1918));

        var d = NetworkHelper.ClassifyAddress("224.0.0.1");
        Assert.Equal(TraditionalClass.D, d.TraditionalClass);
        Assert.True(d.Kind.HasFlag(AddressKind.Multicast));

        var e = NetworkHelper.ClassifyAddress("240.0.0.1");
        Assert.Equal(TraditionalClass.E, e.TraditionalClass);
    }

    [Fact]
    public void Classify_ipv6_has_no_class()
    {
        var docs = NetworkHelper.ClassifyAddress("2001:db8::1");
        Assert.Equal(TraditionalClass.None, docs.TraditionalClass);
        Assert.True(docs.Kind.HasFlag(AddressKind.Documentation));
        Assert.Equal(AddressFamily.InterNetworkV6, docs.Family);
    }

    [Fact]
    public void Describe_ipv4_slash24()
    {
        var block = NetworkHelper.DescribePrefix("192.168.10.37/24");
        Assert.Equal("192.168.10.0", block.Network);
        Assert.Equal(24, block.PrefixLength);
        Assert.Equal("255.255.255.0", block.SubnetMask);
        Assert.Equal(Ipv4Prefix.MaskFromPrefix(24), block.SubnetMask);
        Assert.Equal("192.168.10.255", block.Broadcast);
        Assert.Equal("192.168.10.1", block.FirstUsable);
        Assert.Equal("192.168.10.254", block.LastUsable);
        Assert.Equal(256, block.TotalAddresses);
        Assert.Equal(254, block.UsableHosts);
        Assert.Equal(TraditionalClass.C, block.TraditionalClass);
    }

    [Fact]
    public void Describe_from_dotted_mask_agrees()
    {
        var block = NetworkHelper.DescribePrefix("10.8.1.9", "255.255.255.0");
        Assert.Equal(24, block.PrefixLength);
        Assert.Equal("255.255.255.0", block.SubnetMask);
        Assert.Equal("10.8.1.0", block.Network);
    }

    [Fact]
    public void Plan_by_hosts_200_from_slash16()
    {
        var plan = NetworkHelper.PlanByHosts("10.8.0.0/16", 200);
        Assert.Equal(24, plan.ChildPrefix);
        Assert.Equal(256, plan.TotalNetworks);
        Assert.Equal("10.8.0.0", plan.Networks[0].Network);
        Assert.Equal("255.255.255.0", plan.Networks[0].SubnetMask);
    }

    [Fact]
    public void Plan_by_networks_25_from_slash16()
    {
        var plan = NetworkHelper.PlanByNetworks("10.8.0.0/16", 25);
        Assert.Equal(21, plan.ChildPrefix);
        Assert.Equal(32, plan.TotalNetworks);
        Assert.Equal("10.8.0.0", plan.Networks[0].Network);
        Assert.Equal(21, plan.Networks[0].PrefixLength);
    }

    [Fact]
    public void Pack_vlsm_example()
    {
        var plan = NetworkHelper.PackVlsm("10.8.0.0/16", [200, 50, 12, 2]);
        Assert.Equal(4, plan.Networks.Count);
        Assert.Equal(24, plan.Networks[0].PrefixLength);
        Assert.Equal(26, plan.Networks[1].PrefixLength);
        Assert.Equal(28, plan.Networks[2].PrefixLength);
        Assert.Equal(31, plan.Networks[3].PrefixLength);
        Assert.Equal("10.8.0.0", plan.Networks[0].Network);
        Assert.Equal("10.8.1.0", plan.Networks[1].Network);
        Assert.Equal("10.8.1.64", plan.Networks[2].Network);
        Assert.Equal("10.8.1.80", plan.Networks[3].Network);
    }

    [Fact]
    public void Describe_ipv6_slash32()
    {
        var block = NetworkHelper.DescribePrefix("2001:db8::/32");
        Assert.Null(block.Broadcast);
        Assert.Null(block.SubnetMask);
        Assert.Equal(TraditionalClass.None, block.TraditionalClass);
        Assert.Equal(IPAddress.Parse("2001:db8::"), IPAddress.Parse(block.Network));
        Assert.Equal(IPAddress.Parse("2001:db8::"), IPAddress.Parse(block.FirstUsable!));
        Assert.Equal(IPAddress.Parse("2001:db8:ffff:ffff:ffff:ffff:ffff:ffff"), IPAddress.Parse(block.LastUsable!));
        Assert.True(block.Kind.HasFlag(AddressKind.Documentation));
    }

    [Fact]
    public void Plan_ipv6_networks_from_slash48()
    {
        var plan = NetworkHelper.PlanByNetworks("2001:db8::/48", 25);
        Assert.Equal(53, plan.ChildPrefix);
        Assert.Equal(32, plan.TotalNetworks);
    }

    [Fact]
    public void Contains_and_overlaps()
    {
        Assert.True(NetworkHelper.Contains("10.8.0.0/20", "10.8.3.9"));
        Assert.False(NetworkHelper.Contains("10.8.0.0/20", "10.8.16.1"));
        Assert.True(NetworkHelper.Overlaps("10.8.0.0/20", "10.8.8.0/21"));
        Assert.False(NetworkHelper.Overlaps("10.8.0.0/20", "10.9.0.0/20"));
        Assert.True(NetworkHelper.Contains("2001:db8::/32", "2001:db8:1::1"));
        Assert.False(NetworkHelper.Contains("2001:db8::/32", "2001:db9::1"));
    }

    [Fact]
    public void Split_respects_max_list()
    {
        var plan = NetworkHelper.SplitPrefix("10.0.0.0/8", 28, new SubnetQuery { MaxList = 8 });
        Assert.Equal(8, plan.Networks.Count);
        Assert.Equal(BigIntegerPower(2, 20), plan.TotalNetworks);
    }

    [Fact]
    public void Parent_too_small_throws()
    {
        Assert.Throws<InvalidOperationException>(() => NetworkHelper.PlanByHosts("192.168.1.0/28", 200));
    }

    [Fact]
    public void Next_block_walks_adjacent()
    {
        var next = NetworkHelper.NextBlock("192.168.10.0/24");
        Assert.Equal("192.168.11.0", next!.Network);
    }

    static System.Numerics.BigInteger BigIntegerPower(int two, int exp)
        => System.Numerics.BigInteger.One << exp;
}
