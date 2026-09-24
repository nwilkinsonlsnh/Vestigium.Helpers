using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PathMtuTests
{
    [Fact]
    public void Kind_is_pathMtu()
    {
        var job = NetworkHelper.PathMtu("127.0.0.1", new PathMtuOptions
        {
            MinPayload = 8,
            MaxPayload = 16,
            Timeout = TimeSpan.FromMilliseconds(200)
        });
        Assert.Equal("pathMtu", job.Kind);
    }

    [Fact]
    public void Max_below_min_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PathMtuEngine.Guard(new PathMtuOptions { MinPayload = 100, MaxPayload = 50 }));
    }

    [Fact]
    public void Timeout_is_unknown_not_too_big()
    {
        Assert.Equal(PathMtuOutcome.Unknown, PathMtuEngine.Classify(IcmpEchoStatus.TimedOut, "timeout"));
        Assert.Equal(PathMtuOutcome.Unknown, PathMtuEngine.Classify(IcmpEchoStatus.ProtocolForbidden, null));
        Assert.Equal(PathMtuOutcome.TooBig, PathMtuEngine.Classify(IcmpEchoStatus.DestinationUnreachable, null));
        Assert.Equal(PathMtuOutcome.TooBig, PathMtuEngine.Classify(IcmpEchoStatus.Failed, "PacketTooBig"));
        Assert.Equal(PathMtuOutcome.Passed, PathMtuEngine.Classify(IcmpEchoStatus.Success, null));
    }

    [Fact]
    public void Unknown_does_not_lower_the_ceiling()
    {
        var lo = 8;
        var hi = 32;
        int? largest = null;
        var unknown = new HashSet<int>();
        PathMtuEngine.Step(PathMtuOutcome.Unknown, 20, ref lo, ref hi, ref largest, unknown);
        Assert.Equal(8, lo);
        Assert.Equal(32, hi);
        Assert.Null(largest);
        Assert.Contains(20, unknown);
        Assert.Equal(8, PathMtuEngine.NextSize(lo, hi, unknown));
    }

    [Fact]
    public async Task Loopback_finds_a_passing_size_or_fails_clean()
    {
        var job = NetworkHelper.PathMtu("127.0.0.1", new PathMtuOptions
        {
            MinPayload = 8,
            MaxPayload = 32,
            Timeout = TimeSpan.FromMilliseconds(400)
        });
        var result = await job.RunAsync();
        Assert.True(result.Tries.Count >= 1);
        if (result.LargestPayload is { } size)
        {
            Assert.InRange(size, 8, 32);
            Assert.Contains(result.Tries, t => t.Passed && t.Payload == size);
        }
    }
}
