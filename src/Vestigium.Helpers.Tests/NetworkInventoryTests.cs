using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkInventoryTests
{
    [Fact]
    public void Network_subcategories_are_registered()
    {
        var sub = typeof(Vestigium.Helpers.Network.HelperLog).GetNestedType("Subcategories")
            ?? throw new InvalidOperationException("Network HelperLog.Subcategories is missing.");
        var names = sub.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

        string? catalog = null;
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var hit = Path.Combine(dir.FullName, "src", "Vestigium.Helpers.Network", "NetworkCatalog.cs");
            if (File.Exists(hit))
            {
                catalog = File.ReadAllText(hit);
                break;
            }
        }

        Assert.NotNull(catalog);
        foreach (var name in names)
            Assert.Contains($"\"{name}\"", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void GetWorkstation_includes_loopback_with_address()
    {
        var box = NetworkHelper.GetWorkstation();
        Assert.False(string.IsNullOrWhiteSpace(box.HostName));
        Assert.NotEmpty(box.Adapters);
        Assert.Contains(
            box.Adapters,
            a => a.UnicastAddresses.Any(u =>
                u.Address is "127.0.0.1" or "::1"
                || IPAddress.TryParse(u.Address, out var ip) && IPAddress.IsLoopback(ip)));
    }

    [Fact]
    public void Ipv4_unicast_prefix_and_mask_agree()
    {
        var adapters = NetworkHelper.GetAdapters();
        var rows = adapters.SelectMany(a => a.UnicastAddresses)
            .Where(u => u.Family == AddressFamily.InterNetwork)
            .ToList();
        Assert.NotEmpty(rows);
        foreach (var row in rows)
        {
            Assert.InRange(row.PrefixLength, 0, 32);
            Assert.False(string.IsNullOrWhiteSpace(row.SubnetMask));
            var expected = Ipv4PrefixCheck(row.PrefixLength);
            Assert.Equal(expected, row.SubnetMask);
        }
    }

    [Fact]
    public void GetAdapter_unknown_throws()
    {
        Assert.Throws<ArgumentException>(() => NetworkHelper.GetAdapter("no-such-adapter-vestigium-phase1"));
    }

    [Fact]
    public void GetAdapter_round_trips_existing_name()
    {
        var first = NetworkHelper.GetAdapters().First();
        var again = NetworkHelper.GetAdapter(first.Name);
        Assert.Equal(first.Id, again.Id);
        Assert.Equal(first.Name, again.Name);
    }

    [Fact]
    public void Inventory_exposes_search_list_and_adapter_extras()
    {
        var box = NetworkHelper.GetWorkstation();
        Assert.NotNull(box.DnsSuffixSearchList);
        Assert.NotEmpty(box.Adapters);
        foreach (var adapter in box.Adapters)
        {
            Assert.NotNull(adapter.WinsServers);
            Assert.NotNull(adapter.DnsServers);
            if (adapter.InterfaceIndex is int index)
                Assert.True(index >= 0);
            if (adapter.Ipv4Metric is int metric)
                Assert.True(metric >= 0);
            if (adapter.Mtu is int mtu)
                Assert.True(mtu > 0);
        }
    }

    [Fact]
    public void Linux_netbios_is_unknown()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        foreach (var adapter in NetworkHelper.GetAdapters())
            Assert.Equal(NetbiosOverTcp.Unknown, adapter.NetbiosOverTcp);
    }

    [Theory]
    [InlineData(8, "255.0.0.0")]
    [InlineData(16, "255.255.0.0")]
    [InlineData(24, "255.255.255.0")]
    [InlineData(32, "255.255.255.255")]
    [InlineData(0, "0.0.0.0")]
    public void MaskFromPrefix_known_values(int prefix, string mask)
    {
        Assert.Equal(mask, Ipv4Prefix.MaskFromPrefix(prefix));
        Assert.Equal(prefix, Ipv4Prefix.PrefixFromMask(IPAddress.Parse(mask)));
    }

    private static string Ipv4PrefixCheck(int prefix) => Ipv4Prefix.MaskFromPrefix(prefix);
}
