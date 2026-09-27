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
    public void PC01_001_live_does_not_fake_known_when_missing()
    {
        var missing = new ScriptedInventory { Present = false };
        Assert.False(CpuCounterCatalog.CategoryPresent(CpuObjects.Processor, missing));
        Assert.False(CpuCounterCatalog.HasCounter(CpuObjects.Processor, "% Processor Time", "_Total", missing));
        Assert.False(CpuCounterCatalog.HasInstance(CpuObjects.Processor, "_Total", missing));
        Assert.Empty(CpuCounterCatalog.LiveCounters(CpuObjects.Processor, "_Total", 256, missing));
        Assert.Empty(CpuCounterCatalog.LiveInstances(CpuObjects.Processor, 256, missing));
        Assert.False(CpuCounterCatalog.Snapshot(CpuObjects.Processor, inventory: missing).CategoryPresent);
    }

    [Fact]
    public void PC01_001_check_and_snapshot_use_live_inventory()
    {
        var live = new ScriptedInventory
        {
            Present = true,
            Counters = ["% Processor Time", "Parking Status"],
            Instances = ["_Total", "0,0"]
        };
        Assert.True(CpuCounterCatalog.CategoryPresent(CpuObjects.ProcessorInformation, live));
        Assert.True(CpuCounterCatalog.HasCounter(CpuObjects.ProcessorInformation, "Parking Status", "_Total", live));
        Assert.False(CpuCounterCatalog.HasCounter(CpuObjects.ProcessorInformation, "No Such", "_Total", live));
        Assert.True(CpuCounterCatalog.HasInstance(CpuObjects.ProcessorInformation, "0,0", live));
        var snap = CpuCounterCatalog.Snapshot(CpuObjects.ProcessorInformation, inventory: live);
        Assert.True(snap.CategoryPresent);
        Assert.Equal(["% Processor Time", "Parking Status"], snap.Counters);
        Assert.Equal(["_Total", "0,0"], snap.Instances);
    }

    [Fact]
    public async Task PC01_001_watch_subscribes_to_live_counters_and_instances()
    {
        var inventory = new ScriptedInventory
        {
            Present = true,
            Counters = ["% Processor Time"],
            Instances = ["_Total"]
        };
        var hits = new List<CatalogSnapshot>();
        await CpuCounterCatalog.WatchAsync(
            new CatalogWatchOptions
            {
                Category = CpuObjects.Processor,
                Count = 2,
                Interval = TimeSpan.FromSeconds(1),
                EmitOnlyOnChange = false,
                Inventory = inventory,
                Clock = new ImmediateClock()
            },
            hits.Add);

        Assert.Equal(2, hits.Count);
        Assert.All(hits, s => Assert.Equal(["% Processor Time"], s.Counters));
        Assert.All(hits, s => Assert.Equal(["_Total"], s.Instances));
    }

    [Fact]
    public void PC01_001_is_known_is_not_live()
    {
        Assert.True(CpuCounterCatalog.IsKnownCounter(CpuObjects.Processor, "% Processor Time"));
        Assert.False(CpuCounterCatalog.IsKnownCounter(CpuObjects.Processor, "Parking Status"));
    }

    [Fact]
    public void PC01_002_falls_back_to_processor()
    {
        var noPi = new ScriptedInventory
        {
            Present = true,
            Only = [CpuObjects.Processor]
        };
        var paths = CpuPaths.DefaultJob("_Total", noPi);
        Assert.All(paths.Where(p => p.Counter != CpuPaths.QueueLength), p =>
            Assert.Equal(CpuObjects.Processor, p.Category));
        var queue = Assert.Single(paths, p => p.Counter == CpuPaths.QueueLength);
        Assert.Equal(CpuPaths.SystemObject, queue.Category);
        Assert.Equal("", queue.Instance);
        Assert.Equal("count", queue.Unit);
        Assert.Equal(CpuObjects.Processor, CpuPaths.UtilizationObject(noPi));
    }

    [Fact]
    public void PC01_002_prefers_processor_information()
    {
        var both = new ScriptedInventory
        {
            Present = true,
            Only = [CpuObjects.Processor, CpuObjects.ProcessorInformation]
        };
        var paths = CpuPaths.DefaultJob("0,0", both);
        Assert.Equal(CpuObjects.ProcessorInformation, CpuPaths.UtilizationObject(both));
        Assert.All(paths.Where(p => p.Counter != CpuPaths.QueueLength), p =>
        {
            Assert.Equal(CpuObjects.ProcessorInformation, p.Category);
            Assert.Equal("0,0", p.Instance);
        });
        Assert.Contains(paths, p => p.Category == CpuPaths.SystemObject && p.Counter == CpuPaths.QueueLength);
    }

    [Fact]
    public void PC01_001_empty_instance_becomes_total()
    {
        Assert.Equal("_Total", CpuPaths.InstanceOrTotal(""));
        Assert.Equal("_Total", CpuPaths.InstanceOrTotal("   "));
        Assert.Equal("_Total", CpuPaths.InstanceOrTotal(null));
        var paths = CpuPaths.DefaultJob("  ", new ScriptedInventory { Present = true, Only = [CpuObjects.Processor] });
        Assert.All(paths.Where(p => p.Counter != CpuPaths.QueueLength), p => Assert.Equal("_Total", p.Instance));
    }

    [Fact]
    public void PC01_001_named_instance_is_kept()
    {
        Assert.Equal("3,1", CpuPaths.InstanceOrTotal(" 3,1 "));
    }

    private sealed class ScriptedInventory : ICounterInventory
    {
        public bool Present { get; set; }
        public HashSet<string>? Only { get; set; }
        public IReadOnlyList<string> Counters { get; set; } = [];
        public IReadOnlyList<string> Instances { get; set; } = [];

        public bool CategoryPresent(string category)
            => Present && (Only is null || Only.Contains(category));

        public bool InstancePresent(string category, string instance)
            => Present && Instances.Contains(instance, StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<string> LiveCounters(string category, string instance, int cap)
            => Present ? Counters.Take(cap).ToArray() : [];

        public IReadOnlyList<string> LiveInstances(string category, int cap)
            => Present ? Instances.Take(cap).ToArray() : [];
    }

    private sealed class ImmediateClock : TimeProvider
    {
        private DateTimeOffset _utc = new(2026, 9, 27, 22, 0, 0, TimeSpan.Zero);

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
