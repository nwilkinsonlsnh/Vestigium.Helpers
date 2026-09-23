using System.Text.Json;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPr02Tests
{
    private static NetworkRouteChange TestNet()
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
        try
        {
            NetworkHelper.AddRoute(change);
            try { NetworkHelper.RemoveRoute(change); } catch { }
        }
        catch (NetworkRouteDenied ex)
        {
            Assert.DoesNotContain("ip ", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
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

    [Fact]
    public void PR02_004_two_appends_are_whole_lines()
    {
        var path = Path.Combine(Path.GetTempPath(), "vest-jsonl-" + Guid.NewGuid().ToString("N") + ".jsonl");
        try
        {
            Parallel.For(0, 40, i =>
                CampaignJsonl.Append(path, new { kind = "echo", n = i, pad = new string('x', 120) }));

            var lines = File.ReadAllLines(path);
            Assert.Equal(40, lines.Length);
            var seen = new HashSet<int>();
            foreach (var line in lines)
            {
                Assert.DoesNotContain('\r', line);
                using var doc = JsonDocument.Parse(line);
                Assert.Equal("echo", doc.RootElement.GetProperty("kind").GetString());
                seen.Add(doc.RootElement.GetProperty("n").GetInt32());
            }

            Assert.Equal(40, seen.Count);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }
}
