using System.Reflection;
using Vestigium.Helpers.PerfMon.Cpu;
using Vestigium.Helpers.PerfMon.Disk;
using Vestigium.Helpers.PerfMon.Memory;
using Vestigium.Helpers.PerfMon.PageFile;

namespace Vestigium.Helpers.Tests;

public sealed class PerfMonPr02bTests
{
    [Fact]
    public void PR02b_005_probes_have_no_clr_or_sql_types()
    {
        Assembly[] probes =
        [
            typeof(PagingFile).Assembly,
            typeof(Memory).Assembly,
            typeof(Processor).Assembly,
            typeof(PhysicalDisk).Assembly
        ];

        foreach (var assembly in probes)
        {
            var names = assembly.GetTypes().Select(t => t.Name).ToArray();
            Assert.DoesNotContain(names, n => n.StartsWith("NETCLR", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(names, n => n.StartsWith("SQLServer", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(names, n => n.Equals("Thread", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(names, n => n.Equals("Browser", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void PR02b_005_projects_reference_shared_only()
    {
        foreach (var name in new[]
                 {
                     "Vestigium.Helpers.PerfMon.PageFile.csproj",
                     "Vestigium.Helpers.PerfMon.Memory.csproj",
                     "Vestigium.Helpers.PerfMon.Cpu.csproj",
                     "Vestigium.Helpers.PerfMon.Disk.csproj"
                 })
        {
            var text = File.ReadAllText(Find(name));
            Assert.Contains("Vestigium.Helpers.PerfMon\\Vestigium.Helpers.PerfMon.csproj", text);
            Assert.DoesNotContain("Vestigium.Helpers.Charts", text);
            Assert.DoesNotContain("Vestigium.Helpers.Analytics", text);
            Assert.DoesNotContain("NETCLR", text);
            Assert.DoesNotContain("SQLServer", text);
        }
    }

    private static string Find(string name)
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
}
