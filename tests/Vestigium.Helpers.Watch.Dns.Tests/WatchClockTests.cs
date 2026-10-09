using Vestigium.Helpers.Watch.Dns;
using Xunit;

namespace Vestigium.Helpers.Watch.Dns.Tests;

public sealed class WatchClockTests
{
    [Fact]
    public void Missing_seconds_is_5()
    {
        Assert.True(WatchClock.TryCreate(null, out var clock, out var reject));
        Assert.Null(reject);
        Assert.Equal(5, clock!.Seconds);
        clock.Stop();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(180)]
    public void Step_values_are_accepted(int seconds)
    {
        Assert.True(WatchClock.TryCreate(seconds, out var clock, out _));
        Assert.Equal(seconds, clock!.Seconds);
        clock.Stop();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(181)]
    [InlineData(-5)]
    public void Off_step_values_are_rejected(int seconds)
    {
        Assert.False(WatchClock.TryCreate(seconds, out var clock, out var reject));
        Assert.Null(clock);
        Assert.False(string.IsNullOrWhiteSpace(reject));
    }

    [Fact]
    public async Task Stop_completes_the_clock_without_waiting_the_duration()
    {
        Assert.True(WatchClock.TryCreate(180, out var clock, out _));
        clock!.Stop();
        await clock.Completion.WaitAsync(TimeSpan.FromSeconds(2));
    }
}
