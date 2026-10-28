using Vestigium.Helpers.PerfMon;
using Vestigium.Helpers.PerfMon.Cpu;
using Vestigium.Helpers.PerfMon.Memory;

namespace Vestigium.Helpers.Tests;

public sealed class PerfMonPR03dTests
{
    [Fact]
    public void Cpu_paths_prefer_processor_information_when_that_object_exists()
    {
        var paths = CpuPerf.UtilizationPaths(new Present(CpuObjects.ProcessorInformation, CpuObjects.Processor));
        Assert.Equal(3, paths.Count);
        Assert.All(paths, path => Assert.Equal(CpuObjects.ProcessorInformation, path.Category));
        Assert.Contains(paths, path => path.Counter == Processor.PercentProcessorTime);
        Assert.Contains(paths, path => path.Counter == Processor.PercentUserTime);
        Assert.Contains(paths, path => path.Counter == Processor.PercentPrivilegedTime);
        Assert.DoesNotContain(paths, path => path.Counter == "Processor Queue Length");
    }

    [Fact]
    public void Cpu_paths_fall_back_to_processor_when_information_is_absent()
    {
        var paths = CpuPerf.UtilizationPaths(new Present(CpuObjects.Processor));
        Assert.Equal(3, paths.Count);
        Assert.All(paths, path => Assert.Equal(CpuObjects.Processor, path.Category));
    }

    [Fact]
    public void Memory_host_paths_use_catalog_names()
    {
        var paths = MemoryPerf.HostPaths();
        Assert.Equal(
            [
                Memory.AvailableMBytes,
                Memory.CommittedBytes,
                Memory.PercentCommittedBytesInUse,
                Memory.CommitLimit,
                Memory.CacheBytes
            ],
            paths.Select(path => path.Counter).ToArray());
        Assert.All(paths, path => Assert.Equal(Memory.Category, path.Category));
    }

    [Fact]
    public void Missing_category_is_unavailable_and_does_not_throw()
    {
        using var source = new CachedPdhSource();
        var path = new CounterPath("Vestigium Missing Category", "Bytes", "_Total");
        var record = source.Read(path);
        Assert.Equal(SampleStatus.Unavailable, record.Status);
        Assert.False(source.NeedsPrime(path));
    }

    private sealed class Present : ICounterInventory
    {
        private readonly string[] _categories;

        public Present(params string[] categories) => _categories = categories;

        public bool CategoryPresent(string category)
            => _categories.Contains(category, StringComparer.OrdinalIgnoreCase);

        public bool InstancePresent(string category, string instance) => CategoryPresent(category);

        public IReadOnlyList<string> LiveCounters(string category, string instance, int cap) => [];

        public IReadOnlyList<string> LiveInstances(string category, int cap)
            => CategoryPresent(category) ? ["_Total"] : [];
    }
}
