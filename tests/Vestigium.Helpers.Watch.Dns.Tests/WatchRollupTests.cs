using Vestigium.Helpers.Watch.Dns;
using Xunit;

namespace Vestigium.Helpers.Watch.Dns.Tests;

public sealed class WatchRollupTests
{
    [Fact]
    public void One_hundred_event_queries_are_one_row()
    {
        var rollup = new WatchRollup();
        WatchRow? last = null;
        for (var i = 0; i < 100; i++)
            last = rollup.Add("Edge.Example.", "A", WatchSource.Event, 44);

        Assert.Equal(1, rollup.Keys);
        Assert.Equal(100, last!.ResolverCount);
        Assert.Equal(0, last.PacketCount);
        Assert.Equal(100, last.Total);
        Assert.Equal("edge.example", last.Name);
        Assert.Equal(44, last.Pid);
    }

    [Fact]
    public void A_second_pid_clears_the_caller()
    {
        var rollup = new WatchRollup();
        rollup.Add("edge.example", "A", WatchSource.Event, 44);
        var row = rollup.Add("edge.example", "A", WatchSource.Port, 90);

        Assert.Equal(0, row!.Pid);
        Assert.Equal(1, row.ResolverCount);
        Assert.Equal(1, row.PacketCount);
        Assert.Equal(2, row.Total);
    }

    [Fact]
    public void A_and_aaaa_are_two_keys()
    {
        var rollup = new WatchRollup();
        rollup.Add("edge.example", "A", WatchSource.Event, 1);
        rollup.Add("edge.example", "AAAA", WatchSource.Event, 1);

        Assert.Equal(2, rollup.Keys);
    }

    [Fact]
    public void A_url_does_not_increment()
    {
        var rollup = new WatchRollup();

        Assert.Null(rollup.Add("https://edge.example/a", "A", WatchSource.Event, 1));
        Assert.Equal(0, rollup.Keys);
    }

    [Fact]
    public void Event_then_port_keeps_answers()
    {
        var rollup = new WatchRollup();
        rollup.Add("edge.example", "A", WatchSource.Event, 1, "Success", "1.2.3.4");
        var row = rollup.Add("edge.example", "A", WatchSource.Port, 0);

        Assert.Equal("Success", row!.Status);
        Assert.Equal("1.2.3.4", row.Answers);
        Assert.Equal(1, row.ResolverCount);
        Assert.Equal(1, row.PacketCount);
    }

    [Fact]
    public void Port_then_event_fills_answers()
    {
        var rollup = new WatchRollup();
        rollup.Add("edge.example", "A", WatchSource.Port, 0);
        var row = rollup.Add("edge.example", "A", WatchSource.Event, 1, "Success", "1.2.3.4");

        Assert.Equal("Success", row!.Status);
        Assert.Equal("1.2.3.4", row.Answers);
        Assert.Equal(1, row.ResolverCount);
        Assert.Equal(1, row.PacketCount);
    }

    [Fact]
    public void Empty_does_not_wipe_prior()
    {
        var rollup = new WatchRollup();
        rollup.Add("edge.example", "A", WatchSource.Event, 1, "Success", "1.2.3.4");
        var row = rollup.Add("edge.example", "A", WatchSource.Event, 1, "", "");

        Assert.Equal("Success", row!.Status);
        Assert.Equal("1.2.3.4", row.Answers);
        Assert.Equal(2, row.ResolverCount);
    }
}
