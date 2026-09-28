using System.Diagnostics;
using Vestigium.Helpers.PerfMon;
using Vestigium.Helpers.PerfMon.Memory;

namespace Vestigium.Helpers.Tests;

public sealed class PerfMonMemoryMe01Tests
{
    [Fact]
    public void ME01_001_catalog_lists_five_objects()
    {
        Assert.Equal(MemoryObjects.All, MemoryCounterCatalog.Categories);
        Assert.True(MemoryCounterCatalog.IsKnownCategory("memory"));
        Assert.True(MemoryCounterCatalog.IsKnownCategory(MemoryObjects.HyperVDynamicMemory));
        Assert.True(MemoryCounterCatalog.IsKnownCategory(MemoryObjects.ReadyBoostCache));
        Assert.False(MemoryCounterCatalog.IsKnownCategory("Paging File"));
        Assert.Throws<ArgumentException>(() => MemoryCounterCatalog.Counters("Paging File"));
    }

    [Fact]
    public void ME01_001_known_counters_cover_memory_object()
    {
        Assert.Contains("Available MBytes", MemoryCounterCatalog.Counters(MemoryObjects.Memory));
        Assert.Contains("Committed Bytes", MemoryCounterCatalog.Counters(MemoryObjects.Memory));
        Assert.Contains("Commit Limit", MemoryCounterCatalog.Counters(MemoryObjects.Memory));
        Assert.Contains("% Committed Bytes In Use", MemoryCounterCatalog.Counters(MemoryObjects.Memory));
        Assert.Contains("Pages/sec", MemoryCounterCatalog.Counters(MemoryObjects.Memory));
        Assert.Contains("Copy Read Hits %", MemoryCounterCatalog.Counters(MemoryObjects.Cache));
        Assert.Empty(MemoryCounterCatalog.Counters(MemoryObjects.ReadyBoostCache));
    }

    [Fact]
    public void ME01_001_live_does_not_fake_known_when_missing()
    {
        var missing = new ScriptedInventory { Present = false };
        Assert.False(MemoryCounterCatalog.CategoryPresent(MemoryObjects.Memory, missing));
        Assert.Empty(MemoryCounterCatalog.LiveCounters(MemoryObjects.Memory, "", 256, missing));
        Assert.False(MemoryCounterCatalog.Snapshot(MemoryObjects.Memory, inventory: missing).CategoryPresent);
    }

    [Fact]
    public void ME01_001_check_and_snapshot_use_live_inventory()
    {
        var live = new ScriptedInventory
        {
            Present = true,
            Counters = ["Available MBytes", "Committed Bytes"],
            Instances = [""]
        };
        Assert.True(MemoryCounterCatalog.HasCounter(MemoryObjects.Memory, "Available MBytes", "", live));
        var snap = MemoryCounterCatalog.Snapshot(MemoryObjects.Memory, inventory: live);
        Assert.Equal(["Available MBytes", "Committed Bytes"], snap.Counters);
    }

    [Fact]
    public async Task ME01_001_watch_subscribes()
    {
        var inventory = new ScriptedInventory
        {
            Present = true,
            Counters = ["Available MBytes"],
            Instances = [""]
        };
        var hits = new List<CatalogSnapshot>();
        await MemoryCounterCatalog.WatchAsync(
            new CatalogWatchOptions
            {
                Category = MemoryObjects.Memory,
                Count = 2,
                Interval = TimeSpan.FromSeconds(1),
                EmitOnlyOnChange = false,
                Inventory = inventory,
                Clock = new ImmediateClock()
            },
            hits.Add);
        Assert.Equal(2, hits.Count);
    }

    [Fact]
    public void ME01_002_empty_instance_stays_empty()
    {
        Assert.Equal("", MemoryPaths.InstanceOrNone(null));
        Assert.Equal("", MemoryPaths.InstanceOrNone(""));
        Assert.Equal("", MemoryPaths.InstanceOrNone("   "));
        var paths = MemoryPaths.Short();
        Assert.All(paths, p => Assert.Equal("", p.Instance));
        Assert.DoesNotContain(paths, p => p.Instance.Equals("_Total", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ME01_002_named_instance_is_kept()
    {
        Assert.Equal("0", MemoryPaths.InstanceOrNone(" 0 "));
        var paths = MemoryPaths.Short("0");
        Assert.All(paths, p => Assert.Equal("0", p.Instance));
    }

    [Fact]
    public void ME01_002_paths_are_memory_object()
    {
        var paths = MemoryPaths.Short();
        Assert.Equal(MemoryPaths.ShortCounters, paths.Select(p => p.Counter));
        Assert.All(paths, p => Assert.Equal(MemoryObjects.Memory, p.Category));
        Assert.DoesNotContain(paths, p => p.Category == MemoryPaths.PageFileObject);
        Assert.DoesNotContain(paths, p => p.Category == MemoryObjects.Cache);
        Assert.Equal("MB", paths.First(p => p.Counter == "Available MBytes").Unit);
        Assert.Equal("B", paths.First(p => p.Counter == "Committed Bytes").Unit);
        Assert.Equal("%", paths.First(p => p.Counter == "% Committed Bytes In Use").Unit);
        Assert.Equal("/sec", paths.First(p => p.Counter == "Pages/sec").Unit);
    }

    [Fact]
    public void ME01_003_memory_job_does_not_expand_instances()
    {
        var names = typeof(MemorySampleOptions).GetProperties().Select(p => p.Name);
        Assert.DoesNotContain(names, n => n.Equals("IncludeDisks", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Equals("IncludeCores", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Equals("IncludeAllInstances", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Equals("IncludeNodes", StringComparison.OrdinalIgnoreCase));

        var inv = new ScriptedInventory
        {
            Present = true,
            Instances = ["0", "1", "_Total"]
        };
        var paths = MemoryPaths.For(new MemorySampleOptions { Inventory = inv });
        Assert.Equal(MemoryPaths.ShortCounters.Length, paths.Count);
        Assert.All(paths, p =>
        {
            Assert.Equal(MemoryObjects.Memory, p.Category);
            Assert.Equal("", p.Instance);
        });
        Assert.DoesNotContain(paths, p => p.Category == MemoryObjects.NumaNodeMemory);
    }

    [Fact]
    public void ME01_004_missing_object_stays_memory()
    {
        var missing = new ScriptedInventory { Present = false };
        var paths = MemoryPaths.For(new MemorySampleOptions { Inventory = missing });
        Assert.Equal(MemoryObjects.Memory, MemoryPaths.ObjectName);
        Assert.All(paths, p => Assert.Equal(MemoryObjects.Memory, p.Category));
        Assert.DoesNotContain(paths, p => p.Category == MemoryPaths.PageFileObject);
        Assert.DoesNotContain(paths, p => p.Counter.Contains("% Usage", StringComparison.OrdinalIgnoreCase));
        Assert.False(MemoryCounterCatalog.CategoryPresent(MemoryObjects.Memory, missing));
        Assert.Equal(MemoryPaths.ShortCounters.Length, paths.Count);
    }

    [Fact]
    public async Task ME01_005_run_uses_shared_job()
    {
        var paths = MemoryPaths.Short();
        var fake = new FakeCounterSource();
        foreach (var path in paths)
            fake.Seed(path, SampleRecord.Ok(path, 12));

        var result = await MemoryPerf.RunAsync(new MemorySampleOptions
        {
            Count = 1,
            Source = fake,
            Clock = new ImmediateClock()
        });

        Assert.Equal(SampleStatus.Ok, result.Status);
        Assert.Equal(paths.Count, result.Samples.Count);
        Assert.All(result.Samples, s =>
        {
            Assert.Equal(MemoryObjects.Memory, s.Category);
            Assert.Equal(12, s.Value);
        });
    }

    [Fact]
    public void ME01_005_live_available()
    {
        try
        {
            if (!PerformanceCounterCategory.Exists(MemoryObjects.Memory))
                return;
        }
        catch (InvalidOperationException)
        {
            return;
        }
        catch (ArgumentException)
        {
            return;
        }

        var path = new CounterPath(MemoryObjects.Memory, "Available MBytes", "", "MB");
        var source = new PerformanceCounterSource();
        var row = source.Read(path);
        if (row.Status != SampleStatus.Ok)
            return;
        Assert.NotNull(row.Value);
    }

    private sealed class ScriptedInventory : ICounterInventory
    {
        public bool Present { get; set; }
        public IReadOnlyList<string> Counters { get; set; } = [];
        public IReadOnlyList<string> Instances { get; set; } = [];

        public bool CategoryPresent(string category) => Present;

        public bool InstancePresent(string category, string instance)
            => Present && (string.IsNullOrEmpty(instance) || Instances.Contains(instance, StringComparer.OrdinalIgnoreCase));

        public IReadOnlyList<string> LiveCounters(string category, string instance, int cap)
            => Present ? Counters.Take(cap).ToArray() : [];

        public IReadOnlyList<string> LiveInstances(string category, int cap)
            => Present ? Instances.Take(cap).ToArray() : [];
    }

    private sealed class ImmediateClock : TimeProvider
    {
        private DateTimeOffset _utc = new(2026, 9, 28, 1, 30, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utc;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            if (dueTime != Timeout.InfiniteTimeSpan)
            {
                if (dueTime > TimeSpan.Zero)
                    _utc += dueTime;
                callback(state);
            }

            return new DoneTimer();
        }

        private sealed class DoneTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => false;
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
