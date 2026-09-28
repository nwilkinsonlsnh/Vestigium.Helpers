using System.Text.Json;
using Vestigium.Helpers.PerfMon.Disk;
using Vestigium.Helpers.PerfMon.Memory;
using Vestigium.Helpers.PerfMon.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PerfMonPr03cTests
{
    [Fact]
    public void PR03c_001_remainder_objects_are_allow_listed_not_generated()
    {
        AssertShard(
            "PerfMon.Memory",
            present: ["Cache", "Memory", "NUMA Node Memory", "ReadyBoost Cache"],
            allowOnly: ["Hyper-V Dynamic Memory Integration Service"]);

        AssertShard(
            "PerfMon.Disk",
            present: ["PhysicalDisk", "LogicalDisk"],
            allowOnly: ["ReFS", "Storport Unit Queue", "VHD Bucketized Performance"]);

        AssertShard(
            "PerfMon.Network",
            present:
            [
                "Network Adapter",
                "Network Interface",
                "IPv4",
                "IPv6",
                "ICMP",
                "ICMPv6",
                "TCPv4",
                "TCPv6",
                "UDPv4",
                "UDPv6"
            ],
            allowOnly: ["WinNAT", "IPsec Driver", "SMB Server"]);

        Assert.Null(typeof(Memory).Assembly.GetType("Vestigium.Helpers.PerfMon.Memory.HyperVDynamicMemoryIntegrationService"));
        Assert.Null(typeof(PhysicalDisk).Assembly.GetType("Vestigium.Helpers.PerfMon.Disk.ReFS"));
        Assert.Null(typeof(NetworkAdapter).Assembly.GetType("Vestigium.Helpers.PerfMon.Network.WinNAT"));
    }

    private static void AssertShard(string probe, string[] present, string[] allowOnly)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(FindShard(probe)));
        var cats = doc.RootElement.GetProperty("categories").EnumerateArray()
            .Select(c => c.GetProperty("category").GetString())
            .ToArray();
        var allow = doc.RootElement.GetProperty("allowList").EnumerateArray()
            .Select(e => e.GetString())
            .ToArray();

        foreach (var name in present)
            Assert.Contains(name, cats);
        foreach (var name in allowOnly)
        {
            Assert.Contains(name, allow);
            Assert.DoesNotContain(name, cats);
        }
    }

    private static string FindShard(string probe)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var hits = dir.GetFiles("pdh-categories.json", SearchOption.AllDirectories)
                .Where(f => f.FullName.Contains(probe, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (hits.Length > 0)
                return hits[0].FullName;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(probe);
    }
}
