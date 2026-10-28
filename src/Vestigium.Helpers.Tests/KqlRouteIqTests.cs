using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Tests;

public sealed class KqlRouteIqTests
{
    [Fact]
    public void Accept_queries_compile_and_hit_the_owning_row()
    {
        AssertHit(KqlPack.Route, "route.destination == ipaddress(172.16.0.15)", ("route.destination", "172.16.0.15"));
        AssertHit(KqlPack.Route, "route.destination BEGINS WITH ipaddress(172)", ("route.destination", "172.16.0.15"));
        AssertHit(KqlPack.Route, "route.destination CONTAINS ipaddress(16.0)", ("route.destination", "172.16.0.15"));
        AssertHit(KqlPack.Route, "route.gateway == ipaddress(172.16.0.1) && route.prefixlength >= 16", ("route.gateway", "172.16.0.1"), ("route.prefixlength", 16));
        AssertHit(KqlPack.Route, "route.protocol == netmgmt", ("route.protocol", "netmgmt"));
        AssertHit(KqlPack.Route, "route.protocol == route.protocol.netmgmt", ("route.protocol", "NetMgmt"));
        AssertHit(KqlPack.Route, "route.protocol == route.protocol.local", ("route.protocol", "local"));
        AssertHit(KqlPack.Route, "route.protocol == route.protocol(local)", ("route.protocol", "local"));
        AssertHit(KqlPack.Route, "route.protocol == string(netmgmt)", ("route.protocol", "netmgmt"));
        AssertHit(KqlPack.Route, "route.protocol CONTAINS string(mgm)", ("route.protocol", "netmgmt"));
        AssertHit(KqlPack.Route, "route.interfacename CONTAINS 'ethernet'", ("route.interfacename", "Ethernet 2"));
        AssertHit(KqlPack.Route, "(route.protocol == route.protocol.netmgmt || route.protocol == route.protocol.local) && route.prefixlength >= 16", ("route.protocol", "local"), ("route.prefixlength", 24));
        AssertHit(KqlPack.Route, "(route.protocol == route.protocol.netmgmt OR route.protocol == route.protocol.local) AND route.prefixlength >= 16", ("route.protocol", "netmgmt"), ("route.prefixlength", 16));
        AssertHit(KqlPack.Route, "destination BEGINS WITH ipaddress(172)", ("route.destination", "172.16.0.15"));

        AssertHit(KqlPack.Neighbor, "neighbors.address == ipaddress(172.16.0.15)", ("neighbors.address", "172.16.0.15"));
        AssertHit(KqlPack.Neighbor, "neighbors.macaddress == macaddress(00:e0:4c:0f:31:b4)", ("neighbors.macaddress", "00-e0-4c-0f-31-b4"));
        AssertHit(KqlPack.Neighbor, "neighbors.macaddress == macaddress(00-e0-4c-0f-31-b4)", ("neighbors.macaddress", "00e04c0f31b4"));
        AssertHit(KqlPack.Neighbor, "neighbors.macaddress == macaddress(00e04c0f31b4)", ("neighbors.macaddress", "00:e0:4c:0f:31:b4"));
        AssertHit(KqlPack.Neighbor, "neighbors.macaddress CONTAINS macaddress(4c:0f)", ("neighbors.macaddress", "00:e0:4c:0f:31:b4"));
        AssertHit(KqlPack.Neighbor, "neighbors.macaddress CONTAINS macaddress(4c-0f)", ("neighbors.macaddress", "00e04c0f31b4"));
        AssertHit(KqlPack.Neighbor, "neighbors.macaddress CONTAINS macaddress(4c0f)", ("neighbors.macaddress", "00:e0:4c:0f:31:b4"));
        AssertHit(KqlPack.Neighbor, "neighbors.class == a", ("neighbors.class", "A"));
        AssertHit(KqlPack.Neighbor, "neighbors.class == neighbors.class(a)", ("neighbors.class", "a"));
        AssertHit(KqlPack.Neighbor, "neighbors.class == neighbors.class.a", ("neighbors.class", "a"));
        AssertHit(KqlPack.Neighbor, "neighbors.state == reachable", ("neighbors.state", "reachable"));
        AssertHit(KqlPack.Neighbor, "neighbors.state == neighbors.state.reachable", ("neighbors.state", "reachable"));
        AssertHit(KqlPack.Neighbor, "neighbors.isrouter == true", ("neighbors.isrouter", true));
        AssertHit(KqlPack.Neighbor, "neighbors.isrouter == false", ("neighbors.isrouter", false));
        AssertHit(KqlPack.Neighbor, "neighbors.rtt >= 50", ("neighbors.rtt", 50));
        AssertHit(KqlPack.Neighbor, "neighbors.interfacename == 'Ethernet'", ("neighbors.interfacename", "Ethernet"));
        AssertHit(KqlPack.Neighbor, "(neighbors.state == reachable || neighbors.state == static) && neighbors.isrouter == true", ("neighbors.state", "static"), ("neighbors.isrouter", true));

        AssertHit(KqlPack.Connection, "connections.protocol == tcp", ("connections.protocol", "tcp"));
        AssertHit(KqlPack.Connection, "connections.protocol == udp", ("connections.protocol", "udp"));
        AssertHit(KqlPack.Connection, "connections.protocol == connections.protocol.tcp", ("connections.protocol", "TCP"));
        AssertHit(KqlPack.Connection, "connections.protocol == connections.protocol(udp)", ("connections.protocol", "udp"));
        AssertHit(KqlPack.Connection, "connections.state == established", ("connections.state", "established"));
        AssertHit(KqlPack.Connection, "connections.state == connections.state.established", ("connections.state", "established"));
        AssertHit(KqlPack.Connection, "connections.localport == 443", ("connections.localport", 443));
        AssertHit(KqlPack.Connection, "connections.localport LTE 1000", ("connections.localport", 443));
        AssertHit(KqlPack.Connection, "connections.remoteport == 443", ("connections.remoteport", 443));
        AssertHit(KqlPack.Connection, "connections.service == 'https'", ("connections.service", "https"));
        AssertHit(KqlPack.Connection, "connections.service == '--'", ("connections.service", "--"));
        AssertHit(KqlPack.Connection, "connections.service CONTAINS string(http)", ("connections.service", "https"));
        AssertHit(KqlPack.Connection, "service == 'dns'", ("connections.service", "dns"));
        AssertHit(KqlPack.Connection, "connections.remote BEGINS WITH ipaddress(10.1)", ("connections.remote", "10.1.2.3"));
        AssertHit(KqlPack.Connection, "connections.process CONTAINS 'chrome'", ("connections.process", "chrome.exe"));
        AssertHit(KqlPack.Connection, "connections.process CONTAINS string(chrome)", ("connections.process", "Chrome"));
        AssertHit(KqlPack.Connection, "connections.status == added", ("connections.status", "added"));
        AssertHit(KqlPack.Connection, "connections.status == connections.status.added", ("connections.status", "added"));
        AssertHit(KqlPack.Connection, "connections.time BETWEEN 1 AND 30", ("connections.time", 15));
        AssertHit(KqlPack.Connection, "(connections.protocol == tcp && connections.localport == 443) || (connections.protocol == udp && connections.localport == 53)", ("connections.protocol", "udp"), ("connections.localport", 53));
        AssertHit(KqlPack.Connection, "connections.status == added && (connections.remote BEGINS WITH ipaddress(10) || connections.remote BEGINS WITH ipaddress(172.16))", ("connections.status", "added"), ("connections.remote", "10.0.0.8"));
        AssertHit(KqlPack.Connection, "NOT (connections.state == listen) && connections.protocol == tcp", ("connections.state", "established"), ("connections.protocol", "tcp"));
        AssertHit(KqlPack.Connection, "protocol == tcp", ("connections.protocol", "tcp"));
    }

    [Fact]
    public void Reject_queries_fail_compile_and_do_not_throw()
    {
        AssertReject(KqlPack.Route, "route.protocol == tcp");
        AssertReject(KqlPack.Connection, "connections.protocol == netmgmt");
        AssertReject(KqlPack.Connection, "connections.protocol == route.protocol.netmgmt");
        AssertReject(KqlPack.Neighbor, "neighbors.state == established");
        AssertReject(KqlPack.Connection, "connections.state == reachable");
        AssertReject(KqlPack.Route, "route.destination == ipaddress(172)");
        AssertReject(KqlPack.Neighbor, "neighbors.macaddress == macaddress(4c:0f)");
        AssertReject(KqlPack.Neighbor, "neighbors.macaddress CONTAINS macaddress(4c0)");
        AssertReject(KqlPack.Route, "route.destination == neighbors.address");
        AssertReject(KqlPack.Connection, "connections.remoeport == 443");
        AssertReject(KqlPack.Neighbor, "neighbors.rtt(ms) >= 50");
        AssertReject(KqlPack.Route, "route.protocol == netmgmt & route.prefixlength >= 16");
        AssertReject(KqlPack.Route, "route.protocol == netmgmt | route.protocol == local");
        AssertReject(KqlPack.Connection, "(connections.protocol == tcp && connections.localport == 443");
        AssertReject(KqlPack.Connection, "connections.remote BEGINS WITH ipaddress(10.)");
        AssertReject(KqlPack.Connection, "!(connections.state == listen)");
    }

    [Fact]
    public void Octet_fragment_does_not_hit_a_glued_number()
    {
        using var session = KqlHelper.Create(KqlPack.Route);
        var compiled = KqlHelper.Compile("route.destination CONTAINS ipaddress(16.0)", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        var hit = new KqlFixtureRow(session).Set("route.destination", "172.16.0.15");
        var miss = new KqlFixtureRow(session).Set("route.destination", "172.160.1.1");
        Assert.True(compiled.Query!.Matches(hit));
        Assert.False(compiled.Query.Matches(miss));
    }

    [Fact]
    public void One_group_does_not_satisfy_both_groups()
    {
        using var session = KqlHelper.Create(KqlPack.Connection);
        var compiled = KqlHelper.Compile("(connections.protocol == tcp || connections.protocol == udp) && (connections.localport == 443 || connections.localport == 53)", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        var one = new KqlFixtureRow(session).Set("connections.protocol", "tcp").Set("connections.localport", 80);
        var both = new KqlFixtureRow(session).Set("connections.protocol", "tcp").Set("connections.localport", 443);
        Assert.False(compiled.Query!.Matches(one));
        Assert.True(compiled.Query.Matches(both));
    }

    [Fact]
    public void Process_pack_still_compiles()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("(PID == 0 || Name LIKE '%edge%') && GPU.Usage GT 20", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
    }

    private static void AssertHit(KqlPack pack, string query, params (string Name, object? Value)[] cells)
    {
        using var session = KqlHelper.Create(pack);
        var compiled = KqlHelper.Compile(query, session);
        Assert.True(compiled.Ok, query + " " + compiled.Error);
        var row = new KqlFixtureRow(session);
        foreach (var cell in cells)
            row.Set(cell.Name, cell.Value);
        Assert.True(compiled.Query!.Matches(row), query);
    }

    private static void AssertReject(KqlPack pack, string query)
    {
        using var session = KqlHelper.Create(pack);
        var compiled = KqlHelper.Compile(query, session);
        Assert.False(compiled.Ok, query);
        Assert.NotNull(compiled.Error);
        Assert.True(compiled.Error!.Line >= 1, query);
    }
}
