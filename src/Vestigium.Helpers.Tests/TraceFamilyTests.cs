using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class TraceFamilyTests
{
    [Fact]
    public void Unset_family_keeps_today_resolve()
    {
        var options = new IcmpTraceOptions();
        Assert.Equal(RouteFamily.All, options.Family);
        var job = NetworkHelper.IcmpTrace("127.0.0.1", options);
        Assert.Equal("icmpTrace", job.Kind);
    }

    [Fact]
    public void Ipv4_literal_rejects_ipv6_pin()
    {
        Assert.Throws<ArgumentException>(() =>
            NetworkHelper.IcmpTrace("127.0.0.1", new IcmpTraceOptions { Family = RouteFamily.Pv6 }));
    }

    [Fact]
    public void Ipv6_literal_rejects_ipv4_pin()
    {
        Assert.Throws<ArgumentException>(() =>
            NetworkHelper.IcmpTrace("::1", new IcmpTraceOptions { Family = RouteFamily.Pv4 }));
    }

    [Fact]
    public void Ipv4_literal_accepts_ipv4_pin()
    {
        var job = NetworkHelper.IcmpTrace("127.0.0.1", new IcmpTraceOptions { Family = RouteFamily.Pv4, MaxHops = 1 });
        Assert.Equal("icmpTrace", job.Kind);
    }

    [Fact]
    public void Pick_respects_pin()
    {
        var v4 = System.Net.IPAddress.Parse("192.0.2.1");
        var v6 = System.Net.IPAddress.Parse("2001:db8::1");
        var addrs = new[] { v6, v4 };
        Assert.Equal(v4, TraceResolve.Pick(addrs, RouteFamily.All));
        Assert.Equal(v4, TraceResolve.Pick(addrs, RouteFamily.Pv4));
        Assert.Equal(v6, TraceResolve.Pick(addrs, RouteFamily.Pv6));
        Assert.Null(TraceResolve.Pick(new[] { v4 }, RouteFamily.Pv6));
    }
}
