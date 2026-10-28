using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class TtlSummaryTests
{
    [Fact]
    public void Empty_replies_return_no_estimate()
    {
        var result = NetworkHelper.SummarizeTtl((IEnumerable<IcmpEchoReply>?)null);
        Assert.Equal(0, result.SampleCount);
        Assert.Null(result.MinTtl);
        Assert.Null(result.MaxTtl);
        Assert.Null(result.ModeTtl);
        Assert.Null(result.EstimatedHops);
        Assert.Null(result.AssumedInitialTtl);
    }

    [Fact]
    public void Successful_replies_return_min_max_mode_and_estimated_hops()
    {
        var replies = new[]
        {
            Reply(1, IcmpEchoStatus.Success, 61),
            Reply(2, IcmpEchoStatus.Success, 61),
            Reply(3, IcmpEchoStatus.Success, 60),
            Reply(4, IcmpEchoStatus.TimedOut, 0)
        };

        var result = NetworkHelper.SummarizeTtl(replies);
        Assert.Equal(3, result.SampleCount);
        Assert.Equal(60, result.MinTtl);
        Assert.Equal(61, result.MaxTtl);
        Assert.Equal(61, result.ModeTtl);
        Assert.Equal(64, result.AssumedInitialTtl);
        Assert.Equal(3, result.EstimatedHops);
    }

    [Fact]
    public void Windows_style_ttl_assumes_128()
    {
        var replies = new[]
        {
            Reply(1, IcmpEchoStatus.Success, 120)
        };

        var result = TtlSummary.FromReplies(replies);
        Assert.Equal(128, result.AssumedInitialTtl);
        Assert.Equal(8, result.EstimatedHops);
    }

    [Fact]
    public void Summary_does_not_require_a_trace_job()
    {
        var echo = new IcmpEchoResult(
            "icmp-test",
            "192.0.2.1",
            "192.0.2.1",
            NetworkJobStatus.Success,
            1,
            1,
            0,
            0,
            1,
            1,
            1,
            false,
            [Reply(1, IcmpEchoStatus.Success, 54)]);

        var result = NetworkHelper.SummarizeTtl(echo);
        Assert.Equal(1, result.SampleCount);
        Assert.Equal(10, result.EstimatedHops);
        Assert.Equal(64, result.AssumedInitialTtl);
    }

    private static IcmpEchoReply Reply(int sequence, IcmpEchoStatus status, int ttl)
        => new(sequence, status, "192.0.2.1", 1, ttl, false, null);
}
