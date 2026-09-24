using System.Net.NetworkInformation;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class AdapterWatchTests
{
    [Fact]
    public void Missing_adapter_throws()
    {
        Assert.Throws<ArgumentException>(() => NetworkHelper.WatchAdapter("no-such-nic-" + Guid.NewGuid()));
    }

    [Fact]
    public void Duration_too_short_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AdapterWatchEngine.Guard(new AdapterWatchOptions { Duration = TimeSpan.FromMilliseconds(1) }));
    }

    [Fact]
    public async Task Loopback_watch_returns_samples()
    {
        var nic = NetworkInterface.GetAllNetworkInterfaces().First();
        var job = NetworkHelper.WatchAdapter(nic.Id, new AdapterWatchOptions
        {
            Duration = TimeSpan.FromMilliseconds(40),
            Interval = TimeSpan.FromMilliseconds(15)
        });
        Assert.Equal("watchAdapter", job.Kind);
        var result = await job.RunAsync();
        Assert.True(result.Samples.Count >= 1);
        Assert.Equal(nic.Id, result.AdapterId);
    }
}
