using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PathpingTests
{
    [Fact]
    public void Pathping_kind_is_pathping()
    {
        var job = NetworkHelper.Pathping("127.0.0.1", new PathpingOptions
        {
            MaxHops = 1,
            ProbesPerHop = 1,
            SamplesPerHop = 1,
            Timeout = TimeSpan.FromMilliseconds(200),
            SampleInterval = TimeSpan.Zero
        });
        Assert.Equal("pathping", job.Kind);
    }

    [Fact]
    public void Samples_out_of_range_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.Pathping("127.0.0.1", new PathpingOptions { SamplesPerHop = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.Pathping("127.0.0.1", new PathpingOptions { SamplesPerHop = 101 }));
    }

    [Fact]
    public void Link_loss_is_never_negative()
    {
        Assert.Equal(0, PathpingEngine.LinkLossPercent(40, 10));
        Assert.Equal(20, PathpingEngine.LinkLossPercent(10, 30));
        Assert.Equal(0, PathpingEngine.LinkLossPercent(0, 0));
    }

    [Fact]
    public void Apply_link_loss_fills_difference_to_next_hop()
    {
        var hops = new[]
        {
            new PathpingHop(1, "10.0.0.1", 10, 9, 1, 10, 0, 1, 2, 1.5),
            new PathpingHop(2, "10.0.0.2", 10, 7, 3, 30, 0, 2, 4, 3),
            new PathpingHop(3, "10.0.0.3", 10, 7, 3, 30, 0, 2, 5, 3)
        };
        var rows = PathpingEngine.ApplyLinkLoss(hops);
        Assert.Equal(20, rows[0].LinkLossPercent);
        Assert.Equal(0, rows[1].LinkLossPercent);
        Assert.Equal(0, rows[2].LinkLossPercent);
    }

    [Fact]
    public async Task Loopback_completes()
    {
        var job = NetworkHelper.Pathping("127.0.0.1", new PathpingOptions
        {
            MaxHops = 1,
            ProbesPerHop = 1,
            SamplesPerHop = 1,
            Timeout = TimeSpan.FromSeconds(1),
            SampleInterval = TimeSpan.Zero
        });
        var result = await job.RunAsync();
        Assert.Equal("pathping-", result.JobId[..9]);
        Assert.NotNull(result.Walk);
        Assert.True(result.Hops.Count >= 0);
        Assert.DoesNotContain(result.Hops, h => h.LinkLossPercent < 0);
    }
}
