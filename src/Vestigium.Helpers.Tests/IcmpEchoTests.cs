using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class IcmpEchoTests
{
    [Fact]
    public async Task Default_loopback_sends_four_or_typed_failure()
    {
        var job = NetworkHelper.IcmpEcho("127.0.0.1");
        var result = await job.RunAsync();
        Assert.Equal(4, IcmpEchoOptions.DefaultCount);
        Assert.True(
            result.Sent == 4
            || result.Replies.Any(r => r.Status is IcmpEchoStatus.ProtocolForbidden or IcmpEchoStatus.TimedOut)
            || result.Status is NetworkJobStatus.TimedOut or NetworkJobStatus.Failed,
            $"unexpected {result.Status} sent={result.Sent}");
        Assert.InRange(result.Sent, 0, 4);
        if (result.Status == NetworkJobStatus.Success)
            Assert.Equal(4, result.Sent);
    }

    [Fact]
    public async Task Count_two_sends_two_when_protocol_allowed()
    {
        var job = NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions
        {
            Count = 2,
            Timeout = TimeSpan.FromSeconds(2),
            Interval = TimeSpan.FromMilliseconds(50)
        });
        var result = await job.RunAsync();
        if (result.Status == NetworkJobStatus.Success)
        {
            Assert.Equal(2, result.Sent);
            Assert.Equal(2, result.Replies.Count);
        }
        else
        {
            Assert.True(result.Sent <= 2);
        }
    }

    [Fact]
    public async Task Ping_alias_is_icmp_echo()
    {
        var job = NetworkHelper.Ping("127.0.0.1", new IcmpEchoOptions { Count = 1, Interval = TimeSpan.Zero });
        Assert.Equal("icmpEcho", job.Kind);
        var result = await job.RunAsync();
        Assert.InRange(result.Sent, 0, 1);
    }

    [Fact]
    public async Task Continuous_cancel_completes_within_five_seconds()
    {
        var job = NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions
        {
            Count = 0,
            Timeout = TimeSpan.FromMilliseconds(200),
            Interval = TimeSpan.FromMilliseconds(50)
        });
        var run = job.RunAsync();
        await Task.Delay(200);
        job.Cancel();
        var completed = await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(run, completed);
        var result = await run;
        Assert.True(
            result.Status is NetworkJobStatus.Cancelled or NetworkJobStatus.Failed or NetworkJobStatus.TimedOut or NetworkJobStatus.Success,
            result.Status.ToString());
    }

    [Fact]
    public void Count_below_zero_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { Count = -1 }));
    }

    [Fact]
    public void Timeout_out_of_range_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { Timeout = TimeSpan.FromMilliseconds(1) }));
    }

    [Fact]
    public void Blank_target_throws()
    {
        Assert.Throws<ArgumentException>(() => NetworkHelper.IcmpEcho(" "));
    }
}
