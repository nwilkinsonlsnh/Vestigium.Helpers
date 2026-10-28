using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class BoundEchoTests
{
    [Fact]
    public void Unbound_job_kind_stays_icmpEcho()
    {
        var job = NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { Count = 1, Timeout = TimeSpan.FromMilliseconds(200) });
        Assert.Equal("icmpEcho", job.Kind);
    }

    [Fact]
    public async Task Loopback_source_runs_without_throwing_at_the_door()
    {
        var job = NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions
        {
            Count = 1,
            Timeout = TimeSpan.FromMilliseconds(400),
            SourceAddress = "127.0.0.1"
        });
        var result = await job.RunAsync();
        Assert.NotNull(result);
        Assert.Equal(1, result.Sent);
        Assert.Contains(result.Replies[0].Status, new[]
        {
            IcmpEchoStatus.Success,
            IcmpEchoStatus.TimedOut,
            IcmpEchoStatus.Failed,
            IcmpEchoStatus.ProtocolForbidden,
            IcmpEchoStatus.DestinationUnreachable
        });
    }
}
