using System.Reflection;
using Vestigium.Helpers.SystemInfo;
using Vestigium.Helpers.SystemInfo.Cpu;
using MemoryFacts = Vestigium.Helpers.SystemInfo.Memory.MemoryFacts;

namespace Vestigium.Helpers.Tests;

public sealed class SystemInfoPR03gTests
{
    [Fact]
    public void Unavailable_has_no_value()
    {
        var fact = Fact<ulong>.Unavailable();
        Assert.Equal(FactStatus.Unavailable, fact.Status);
        Assert.False(fact.IsOk);
        Assert.Null(fact.Value);
    }

    [Fact]
    public void Records_keep_raw_numbers()
    {
        var cpu = new CpuTopology(1, 2, 4, 1024, 2048, 4096, 0);
        var live = new CpuLive(1, 2, 3, 2400, 3600);
        var memory = new Vestigium.Helpers.SystemInfo.MemoryFacts(1, 2, 3, 4, 5, 6);
        Assert.Equal(1024, cpu.L1Bytes);
        Assert.Equal(2400u, live.CurrentMhz);
        Assert.Equal(1ul, memory.TotalBytes);
    }

    [Fact]
    public void Doors_are_snapshots_not_jobs()
    {
        Assert.Equal(nameof(CpuFacts.Host), "Host");
        Assert.NotNull(typeof(CpuFacts).GetMethod(nameof(CpuFacts.Live)));
        Assert.NotNull(typeof(MemoryFacts).GetMethod(nameof(MemoryFacts.Read)));
        Assert.DoesNotContain(typeof(CpuFacts).GetMethods(), method => method.Name.Contains("SampleJob", StringComparison.Ordinal));
        Assert.DoesNotContain(typeof(MemoryFacts).GetMethods(), method => method.Name.Contains("SampleJob", StringComparison.Ordinal));
    }

    [Fact]
    public void Satellites_do_not_reference_perfmon_or_network()
    {
        AssertNo(typeof(Fact<int>).Assembly);
        AssertNo(typeof(CpuFacts).Assembly);
        AssertNo(typeof(MemoryFacts).Assembly);
    }

    private static void AssertNo(Assembly assembly)
    {
        var names = assembly.GetReferencedAssemblies().Select(name => name.Name ?? string.Empty);
        Assert.DoesNotContain(names, name => name.Contains("PerfMon", StringComparison.Ordinal));
        Assert.DoesNotContain(names, name => name.Contains("Helpers.Network", StringComparison.Ordinal));
    }
}
