using System.Reflection;
using Vestigium.Helpers.SystemInfo;

namespace Vestigium.Helpers.Tests;

public sealed class SystemInfoPR03gTests
{
    [Fact]
    public void Unavailable_has_no_value()
    {
        var fact = Fact<ulong>.Unavailable();
        Assert.Equal(FactStatus.Unavailable, fact.Status);
        Assert.False(fact.IsOk);
        Assert.Throws<InvalidOperationException>(() => _ = fact.Value);
    }

    [Fact]
    public void Zero_is_a_real_measurement_when_ok()
    {
        var fact = Fact<ulong>.Ok(0);
        Assert.True(fact.IsOk);
        Assert.Equal(0ul, fact.Value);
    }

    [Fact]
    public void Records_keep_raw_numbers()
    {
        var cpu = new CpuTopology(1, 2, 4, 1024, 2048, 4096, 0);
        var live = new CpuLive(1, 2, 3, 2400, 3600);
        var memory = new MemoryRead(
            Fact<ulong>.Ok(1),
            Fact<ulong>.Ok(2),
            Fact<ulong>.Ok(3),
            Fact<ulong>.Ok(4),
            Fact<ulong>.Ok(5),
            Fact<ulong>.Ok(6));
        Assert.Equal(1024, cpu.L1Bytes);
        Assert.Equal(2400u, live.CurrentMhz);
        Assert.Equal(1ul, memory.TotalBytes.Value);
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
    public void Package_does_not_reference_perfmon_or_network()
    {
        var assembly = typeof(CpuFacts).Assembly;
        var names = assembly.GetReferencedAssemblies().Select(name => name.Name ?? string.Empty);
        Assert.DoesNotContain(names, name => name.Contains("PerfMon", StringComparison.Ordinal));
        Assert.DoesNotContain(names, name => name.Contains("Helpers.Network", StringComparison.Ordinal));
    }
}
