using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class IcmpTraceTests
{
    [Fact]
    public async Task Trace_loopback_completes_without_throwing()
    {
        var job = NetworkHelper.IcmpTrace("127.0.0.1", new IcmpTraceOptions
        {
            MaxHops = 3,
            ProbesPerHop = 1,
            Timeout = TimeSpan.FromSeconds(1)
        });
        Assert.Equal("icmpTrace", job.Kind);
        var result = await job.RunAsync();
        Assert.InRange(result.HopCount, 1, 3);
        Assert.True(
            result.Reached
            || result.Status is NetworkJobStatus.TimedOut or NetworkJobStatus.Failed or NetworkJobStatus.Success,
            result.Status.ToString());
    }

    [Fact]
    public async Task Trace_alias_is_icmp_trace()
    {
        var job = NetworkHelper.Trace("127.0.0.1", new IcmpTraceOptions
        {
            MaxHops = 1,
            ProbesPerHop = 1,
            Timeout = TimeSpan.FromSeconds(1)
        });
        Assert.Equal("icmpTrace", job.Kind);
        var result = await job.RunAsync();
        Assert.Equal(1, result.Hops.Count);
    }

    [Fact]
    public void Trace_maxhops_out_of_range_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpTrace("127.0.0.1", new IcmpTraceOptions { MaxHops = 0 }));
    }
}
