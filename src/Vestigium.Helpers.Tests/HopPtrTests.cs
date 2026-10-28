using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class HopPtrTests
{
    [Fact]
    public async Task Blank_address_leaves_name_empty()
    {
        Assert.Null(await HopPtr.LookupAsync(null, CancellationToken.None));
        Assert.Null(await HopPtr.LookupAsync("", CancellationToken.None));
        Assert.Null(await HopPtr.LookupAsync("not-an-ip", CancellationToken.None));
    }

    [Fact]
    public async Task Fill_does_not_drop_a_hop_on_miss()
    {
        var hop = new IcmpTraceHop(1, "203.0.113.1", []);
        var filled = await HopPtr.FillTraceAsync([hop], CancellationToken.None);
        Assert.Single(filled);
        Assert.Equal("203.0.113.1", filled[0].Address);
        Assert.Equal(1, filled[0].Ttl);
    }

    [Fact]
    public void Trace_hop_name_defaults_null()
    {
        var hop = new IcmpTraceHop(1, "127.0.0.1", []);
        Assert.Null(hop.Name);
    }
}
