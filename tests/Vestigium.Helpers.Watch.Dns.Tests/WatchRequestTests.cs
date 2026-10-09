using Vestigium.Helpers.Watch.Dns;
using Xunit;

namespace Vestigium.Helpers.Watch.Dns.Tests;

public sealed class WatchRequestTests
{
    [Fact]
    public void A_missing_source_rejects_before_a_sensor()
    {
        Assert.False(WatchRequest.TryCreate(null, null, out var request, out var reject));
        Assert.Null(request);
        Assert.False(string.IsNullOrWhiteSpace(reject));
    }

    [Fact]
    public void Event_with_a_missing_duration_is_5()
    {
        Assert.True(WatchRequest.TryCreate(WatchSource.Event, null, out var request, out _));
        Assert.Equal(WatchSource.Event, request!.Source);
        Assert.Equal(5, request.Clock.Seconds);
        request.Clock.Stop();
    }

    [Fact]
    public void A_bad_duration_rejects()
    {
        Assert.False(WatchRequest.TryCreate(WatchSource.Port, 6, out var request, out var reject));
        Assert.Null(request);
        Assert.False(string.IsNullOrWhiteSpace(reject));
    }

    [Fact]
    public void A_missing_arg_is_both()
    {
        Assert.True(WatchRequest.ParseArgs([], out var request, out _));
        Assert.Equal(WatchSource.Both, request!.Source);
        request.Clock.Stop();
    }

    [Fact]
    public void A_bad_arg_rejects()
    {
        Assert.False(WatchRequest.ParseArgs(["nope"], out var request, out var reject));
        Assert.Null(request);
        Assert.False(string.IsNullOrWhiteSpace(reject));
    }
}
