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
        string[] required =
        [
            HelperLog.Subcategories.Probe,
            HelperLog.Subcategories.Identity,
            HelperLog.Subcategories.Guard,
            HelperLog.Subcategories.Inventory,
            HelperLog.Subcategories.Adapter,
            HelperLog.Subcategories.Icmp,
            HelperLog.Subcategories.Dns,
            HelperLog.Subcategories.Connection,
            HelperLog.Subcategories.Neighbor,
            HelperLog.Subcategories.Netbios,
            HelperLog.Subcategories.Route,
            HelperLog.Subcategories.Snapshot,
            HelperLog.Subcategories.Campaign,
            HelperLog.Subcategories.Job,
            HelperLog.Subcategories.Stats
        ];
        foreach (var sub in required)
            Assert.True(HelperLog.Taxonomy.IsSubcategoryRegistered(HelperLog.Category, sub), sub);
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
