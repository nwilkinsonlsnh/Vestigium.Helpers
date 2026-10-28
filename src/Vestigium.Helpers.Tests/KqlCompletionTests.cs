using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Tests;

public sealed class KqlCompletionTests
{
    [Fact]
    public void Route_prefix_defaults_to_protocol()
    {
        var completion = Complete(KqlPack.Route, "route.prot");
        Assert.Equal(KqlCompletionSlot.Field, completion.Slot);
        Assert.Equal("route.protocol", completion.Rows[0].Insert);
    }

    [Fact]
    public void Operator_slot_offers_equals_and_contains()
    {
        var completion = Complete(KqlPack.Route, "route.protocol ");
        Assert.Equal(KqlCompletionSlot.Operator, completion.Slot);
        Assert.Contains(completion.Rows, row => row.Insert == "==");
        Assert.Contains(completion.Rows, row => row.Insert == "CONTAINS");
    }

    [Fact]
    public void Closed_value_offers_both_spellings()
    {
        var completion = Complete(KqlPack.Route, "route.protocol == ");
        Assert.Equal(KqlCompletionSlot.Value, completion.Slot);
        Assert.Contains(completion.Rows, row => row.Insert == "route.protocol.netmgmt");
        Assert.Contains(completion.Rows, row => row.Insert == "route.protocol(netmgmt)");
        Assert.DoesNotContain(completion.Rows, row => row.Insert.Contains("tcp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Address_operator_offers_the_constructor()
    {
        var completion = Complete(KqlPack.Route, "route.destination BEGINS WITH ");
        Assert.Equal(KqlCompletionSlot.Value, completion.Slot);
        Assert.Equal("ipaddress(", completion.Rows[0].Insert);
    }

    [Fact]
    public void Open_constructor_is_an_empty_list()
    {
        var completion = Complete(KqlPack.Route, "route.destination BEGINS WITH ipaddress(172");
        Assert.Equal(KqlCompletionSlot.None, completion.Slot);
        Assert.Empty(completion.Rows);
    }

    [Fact]
    public void Neighbor_prefix_defaults_to_macaddress()
    {
        var completion = Complete(KqlPack.Neighbor, "neighbors.mac");
        Assert.Equal("neighbors.macaddress", completion.Rows[0].Insert);
    }

    [Fact]
    public void Connection_values_are_tcp_and_udp_not_netmgmt()
    {
        var completion = Complete(KqlPack.Connection, "connections.protocol == ");
        Assert.Contains(completion.Rows, row => row.Insert == "connections.protocol.tcp");
        Assert.Contains(completion.Rows, row => row.Insert == "connections.protocol(udp)");
        Assert.DoesNotContain(completion.Rows, row => row.Insert.Contains("netmgmt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Connection_session_does_not_offer_a_route_field()
    {
        var completion = Complete(KqlPack.Connection, "route.des");
        Assert.DoesNotContain(completion.Rows, row => row.Insert == "route.destination");
    }

    [Fact]
    public void Closed_value_insert_compiles()
    {
        using var session = KqlHelper.Create(KqlPack.Route);
        var completion = KqlHelper.Complete("route.protocol == ", "route.protocol == ".Length, session);
        var insert = completion.Rows.First(row => row.Insert == "route.protocol.netmgmt").Insert;
        var compiled = KqlHelper.Compile("route.protocol == " + insert, session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
    }

    [Fact]
    public void Process_pack_still_completes_pid()
    {
        var completion = Complete(KqlPack.Process, "PI");
        Assert.Contains(completion.Rows, row => row.Insert.Contains("Pid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Negative_caret_returns_an_empty_list()
    {
        using var session = KqlHelper.Create(KqlPack.Route);
        var completion = KqlHelper.Complete("route.protocol", -1, session);
        Assert.Empty(completion.Rows);
    }

    [Fact]
    public void Hint_does_not_beat_a_longer_prefix()
    {
        using var session = KqlHelper.Create(KqlPack.Route);
        var completion = KqlHelper.Complete("route.prot", "route.prot".Length, session, ["route.prefixlength == 16"]);
        Assert.Equal("route.protocol", completion.Rows[0].Insert);
    }

    private static KqlCompletion Complete(KqlPack pack, string text)
    {
        using var session = KqlHelper.Create(pack);
        return KqlHelper.Complete(text, text.Length, session);
    }
}
