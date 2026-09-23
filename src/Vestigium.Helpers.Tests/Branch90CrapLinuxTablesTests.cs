using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90CrapLinuxTablesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vest-proc-" + Guid.NewGuid().ToString("N"));

    public Branch90CrapLinuxTablesTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "proc", "net"));
        NetworkTestHooks.ProcRoot = _root;
    }

    public void Dispose()
    {
        NetworkTestHooks.Reset();
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void Neighbors_routes_and_parsers()
    {
        Assert.Empty(NetworkLinuxTables.GetOwnerPids());
        Assert.Empty(NetworkLinuxTables.GetNeighbors());
        Assert.Empty(NetworkLinuxTables.GetRoutes(RouteFamily.All));

        File.WriteAllText(Path.Combine(_root, "proc", "net", "arp"),
            "IP address       HW type     Flags       HW address            Mask     Device\n" +
            "192.168.1.1      0x1         0x2         aa:bb:cc:dd:ee:ff     *        eth0\n" +
            "192.168.1.2      0x1         0x4         00:00:00:00:00:00     *        eth0\n" +
            "192.168.1.3      0x1         0x8         11:22:33:44:55:66     *        wlan0\n" +
            "192.168.1.4      0x1         0x1         11:22:33:44:55:66     *        eth0\n" +
            "short\n");

        File.WriteAllText(Path.Combine(_root, "proc", "net", "route"),
            "Iface\tDestination\tGateway\tFlags\tRefCnt\tUse\tMetric\tMask\tMTU\tWindow\tIRTT\n" +
            "eth0\t00000000\t0100A8C0\t0003\t0\t0\t100\t00000000\t0\t0\t0\n" +
            "eth0 0000000A 00000000 0001 0 0 0 00FFFFFF 0 0 0\n" +
            "bad\n");

        File.WriteAllText(Path.Combine(_root, "proc", "net", "ipv6_route"),
            "00000000000000000000000000000001 80 00000000000000000000000000000000 00 00000000000000000000000000000000 00000000 00000000 00000000 00000001 lo\n" +
            "short\n" +
            "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz 80 00000000000000000000000000000000 00 00000000000000000000000000000000 00000000 00000000 00000000 00000001 lo\n");

        var neighbors = NetworkLinuxTables.GetNeighbors();
        Assert.True(neighbors.Count >= 3);

        Assert.NotEmpty(NetworkLinuxTables.GetRoutes(RouteFamily.Pv4));
        Assert.NotEmpty(NetworkLinuxTables.GetRoutes(RouteFamily.Pv6));
        Assert.NotEmpty(NetworkLinuxTables.GetRoutes(RouteFamily.All));

        Assert.Equal("Reachable", NetworkLinuxTables.NeighborState(0x02));
        Assert.Equal("Permanent", NetworkLinuxTables.NeighborState(0x04));
        Assert.Equal("Failed", NetworkLinuxTables.NeighborState(0x08));
        Assert.Equal("Incomplete", NetworkLinuxTables.NeighborState(0));

        Assert.False(NetworkLinuxTables.TryParseNeighbor("a b", out _));
        Assert.False(NetworkLinuxTables.TryParseIpv4Route("a", out _));
        Assert.False(NetworkLinuxTables.TryParseIpv6Route("a b c", out _));
        Assert.Equal("0.0.0.0", NetworkLinuxTables.HexIpv4("nothex"));
        Assert.Equal("abcd", NetworkLinuxTables.FormatIpv6Hex("abcd"));
        Assert.Equal(0, NetworkLinuxTables.ParseHex("nope"));
        Assert.Equal(2, NetworkLinuxTables.ParseHex("0x2"));

        var le = NetworkLinuxTables.Ipv4Bytes(0x0100007F, littleEndian: true);
        var be = NetworkLinuxTables.Ipv4Bytes(0x0100007F, littleEndian: false);
        Assert.Equal(4, le.Length);
        Assert.Equal(le.Reverse().ToArray(), be);
        Assert.Equal("127.0.0.1", new System.Net.IPAddress(le).ToString());
    }
}
