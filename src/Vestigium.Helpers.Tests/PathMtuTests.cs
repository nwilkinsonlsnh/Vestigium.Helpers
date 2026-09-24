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
