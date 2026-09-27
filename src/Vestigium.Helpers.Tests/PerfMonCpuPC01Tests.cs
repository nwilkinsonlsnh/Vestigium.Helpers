using Vestigium.Helpers.PerfMon;
using Vestigium.Helpers.PerfMon.Cpu;

namespace Vestigium.Helpers.Tests;

public sealed class PerfMonCpuPc01Tests
{
    [Fact]
    public void PC01_001_catalog_lists_three_objects()
    {
        Assert.Equal(
            ["Processor", "Processor Information", "Processor Performance"],
            CpuCounterCatalog.Categories);
        Assert.True(CpuCounterCatalog.IsKnownCategory("processor information"));
        Assert.False(CpuCounterCatalog.IsKnownCategory("GPU Engine"));
        Assert.Throws<ArgumentException>(() => CpuCounterCatalog.Counters("GPU Engine"));
    }

    [Fact]
    public void PC01_001_known_counters_cover_each_object()
    {
        Assert.Contains("% Processor Time", CpuCounterCatalog.Counters(CpuObjects.Processor));
        Assert.Contains("Parking Status", CpuCounterCatalog.Counters(CpuObjects.ProcessorInformation));
        Assert.Contains("Processor Frequency", CpuCounterCatalog.Counters(CpuObjects.ProcessorPerformance));
        Assert.Contains("Processor Utility", CpuCounterCatalog.Counters(CpuObjects.ProcessorPerformance));
        Assert.DoesNotContain(CpuCounterCatalog.Counters(CpuObjects.Processor), string.IsNullOrWhiteSpace);
    }

    [Fact]
    public void PC01_001_paths_bind_every_known_counter()
    {
        var paths = CpuCounterCatalog.Paths(CpuObjects.ProcessorInformation, "_Total");
        Assert.Equal(CpuCounterCatalog.Counters(CpuObjects.ProcessorInformation).Count, paths.Count);
        Assert.All(paths, p =>
        {
            Assert.Equal("Processor Information", p.Category);
            Assert.Equal("_Total", p.Instance);
            Assert.False(string.IsNullOrWhiteSpace(p.Counter));
        });
        Assert.Equal("%", paths.First(p => p.Counter == "% Processor Time").Unit);
        Assert.Equal("flag", paths.First(p => p.Counter == "Parking Status").Unit);
        Assert.Equal("MHz", paths.First(p => p.Counter == "Processor Frequency").Unit);
    }

    [Fact]
    public void PC01_001_live_counters_do_not_throw_when_missing()
    {
        var live = CpuCounterCatalog.LiveCounters(CpuObjects.ProcessorPerformance, "_Total", 8);
        Assert.InRange(live.Count, 1, 8);
        Assert.Empty(CpuCounterCatalog.LiveCounters(CpuObjects.Processor, "_Total", 0));
        Assert.Empty(CpuCounterCatalog.LiveInstances(CpuObjects.Processor, 0));
    }
}
