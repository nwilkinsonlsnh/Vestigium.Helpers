using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR02Tests
{
    static NetworkRouteChange TestNet()
        => new()
        {
            Destination = "192.0.2.0",
            PrefixLength = 24,
            Gateway = "192.0.2.1",
            InterfaceIndex = 1
        };

    [Fact]
    public void PR02_002_linux_add_route_is_network_route_denied()
    {
        if (OperatingSystem.IsWindows())
            return;

        var change = TestNet();
        var ex = Assert.Throws<NetworkRouteDenied>(() => NetworkHelper.AddRoute(change));
        Assert.Contains("Linux", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not in v1", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<NetworkRouteDenied>(() => NetworkHelper.ChangeRoute(change));
        Assert.Throws<NetworkRouteDenied>(() => NetworkHelper.RemoveRoute(change));
        Assert.Throws<NetworkRouteDenied>(() => NetworkHelper.DeleteRoute(change));
    }

    [Fact]
    public void PR02_002_windows_add_route_without_admin_is_denied()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var change = new NetworkRouteChange
        {
            Destination = "192.0.2.250",
            PrefixLength = 32,
            Gateway = "192.0.2.1",
            InterfaceIndex = NetworkRouteMutation.TryFirstIpv4Index() ?? 1,
            Persistent = false
        };

        try
        {
            NetworkHelper.AddRoute(change);
            try { NetworkHelper.RemoveRoute(change); } catch { }
        }
        catch (NetworkRouteDenied)
        {
        }
    }

    [Fact]
    public void PR02_003_label_over_63_rejected()
    {
        var label = new string('a', 64);
        var name = label + ".example.test";
        Assert.Throws<ArgumentException>(() => DnsWireName.Guard(name));
        Assert.Throws<ArgumentException>(() => DnsClient.EncodeQuery(1, name, DnsRecordType.A, true));
    }

    [Fact]
    public void PR02_003_name_over_255_rejected()
    {
        var label = new string('a', 63);
        var name = string.Join('.', label, label, label, label);
        var ex = Assert.Throws<ArgumentException>(() => DnsWireName.Guard(name));
        Assert.Contains("255", ex.Message);
        Assert.Throws<ArgumentException>(() => DnsClient.EncodeQuery(1, name, DnsRecordType.A, true));
    }

    [Fact]
    public void PR02_003_non_ascii_and_nul_rejected()
    {
        Assert.Throws<ArgumentException>(() => DnsWireName.Guard("café.example.test"));
        Assert.Throws<ArgumentException>(() => DnsWireName.Guard("bad\0label.example.test"));
        Assert.Throws<ArgumentException>(() => DnsClient.EncodeQuery(1, "café.example.test", DnsRecordType.A, true));
    }

    [Fact]
    public void PR02_003_punycode_and_short_name_ok()
    {
        DnsWireName.Guard("www.example.test");
        DnsWireName.Guard("xn--caf-dma.example.test");
        var wire = DnsClient.EncodeQuery(1, "www.example.test", DnsRecordType.A, true);
        Assert.True(wire.Length > 12);
    }
}
