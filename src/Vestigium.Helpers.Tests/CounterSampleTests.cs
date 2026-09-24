using System.Net.NetworkInformation;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class CounterSampleTests
{
    [Fact]
    public void Duration_out_of_range_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CounterSampleEngine.Guard(new CounterSampleOptions { Duration = TimeSpan.Zero }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CounterSampleEngine.Guard(new CounterSampleOptions { Duration = TimeSpan.FromHours(2) }));
    }

    [Fact]
    public void Interval_longer_than_duration_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CounterSampleEngine.Guard(new CounterSampleOptions
            {
                Duration = TimeSpan.FromSeconds(1),
                Interval = TimeSpan.FromSeconds(2)
            }));
    }

    [Fact]
    public void Missing_adapter_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            NetworkHelper.SampleCounters("no-such-adapter-zzzz", new CounterSampleOptions { Duration = TimeSpan.FromMilliseconds(10) }));
    }

    [Fact]
    public void Delta_does_not_go_negative()
    {
        var start = new CounterReading(100, 50, 2, 1, 3, 4);
        var end = new CounterReading(90, 60, 1, 2, 3, 1);
        var delta = CounterSampleEngine.Delta(start, end);
        Assert.Equal(0, delta.BytesIn);
        Assert.Equal(10, delta.BytesOut);
        Assert.Equal(0, delta.ErrorsIn);
        Assert.Equal(1, delta.ErrorsOut);
        Assert.Equal(0, delta.DiscardsIn);
        Assert.Equal(0, delta.DiscardsOut);
    }

    [Fact]
    public async Task First_up_adapter_samples()
    {
        var nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up);
        if (nic is null)
            return;

        var job = NetworkHelper.SampleCounters(nic.Name, new CounterSampleOptions { Duration = TimeSpan.FromMilliseconds(20) });
        Assert.Equal("counterSample", job.Kind);
        var result = await job.RunAsync();
        Assert.Equal(nic.Name, result.AdapterName);
        Assert.True(result.Delta.BytesIn >= 0);
        Assert.True(result.Delta.BytesOut >= 0);
        Assert.Empty(result.Samples);
    }
}
