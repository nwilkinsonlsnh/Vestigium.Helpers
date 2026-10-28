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

    [Fact]
    public void PR03c_004_probes_stay_shared_only()
    {
        foreach (var name in new[]
                 {
                     "Vestigium.Helpers.PerfMon.Memory.csproj",
                     "Vestigium.Helpers.PerfMon.Disk.csproj",
                     "Vestigium.Helpers.PerfMon.Network.csproj",
                     "Vestigium.Helpers.PerfMon.Gpu.csproj"
                 })
        {
            var text = File.ReadAllText(FindProject(name));
            Assert.Contains("Vestigium.Helpers.PerfMon\\Vestigium.Helpers.PerfMon.csproj", text);
            Assert.DoesNotContain("Vestigium.Helpers.Network\\", text);
            Assert.DoesNotContain("Vestigium.Helpers.Charts", text);
            Assert.DoesNotContain("Vestigium.Helpers.Analytics", text);
            Assert.DoesNotContain("SQLServer", text);
            Assert.DoesNotContain("NETCLR", text);
        }
    }

    [Fact]
    public void PR03c_003_missing_objects_have_no_invented_types()
    {
        string[] missing =
        [
            "Vestigium.Helpers.PerfMon.Memory.HyperVDynamicMemoryIntegrationService",
            "Vestigium.Helpers.PerfMon.Disk.ReFS",
            "Vestigium.Helpers.PerfMon.Disk.StorportUnitQueue",
            "Vestigium.Helpers.PerfMon.Disk.VHDBucketizedPerformance",
            "Vestigium.Helpers.PerfMon.Network.WinNAT",
            "Vestigium.Helpers.PerfMon.Network.IPsecDriver",
            "Vestigium.Helpers.PerfMon.Network.SMBServer",
            "Vestigium.Helpers.PerfMon.Network.HTTPService",
            "Vestigium.Helpers.PerfMon.Gpu.GPUEngine"
        ];
        var assemblies = new[]
        {
            typeof(Memory).Assembly,
            typeof(PhysicalDisk).Assembly,
            typeof(NetworkAdapter).Assembly
        };
        foreach (var name in missing)
            Assert.DoesNotContain(assemblies, a => a.GetType(name) is not null);
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

    private static string FindProject(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var hit = dir.GetFiles(name, SearchOption.AllDirectories).FirstOrDefault();
            if (hit is not null)
                return hit.FullName;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(name);
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
