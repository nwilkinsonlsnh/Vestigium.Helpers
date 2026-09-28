using Vestigium.Helpers.PerfMon;
using Vestigium.Helpers.PerfMon.Gpu;

namespace Vestigium.Helpers.Tests;

public sealed class PerfMonGpuPg01Tests
{
    [Fact]
    public void PG01_001_catalog_lists_five_objects()
    {
        Assert.Equal(GpuObjects.All, GpuCounterCatalog.Categories);
        Assert.True(GpuCounterCatalog.IsKnownCategory("gpu engine"));
        Assert.True(GpuCounterCatalog.IsKnownCategory(GpuObjects.NonLocalAdapterMemory));
        Assert.False(GpuCounterCatalog.IsKnownCategory("PhysicalDisk"));
        Assert.Throws<ArgumentException>(() => GpuCounterCatalog.Counters("PhysicalDisk"));
    }

    [Fact]
    public void PG01_001_known_counters_cover_each_object()
    {
        Assert.Contains("Utilization Percentage", GpuCounterCatalog.Counters(GpuObjects.Engine));
        Assert.Contains("Dedicated Usage", GpuCounterCatalog.Counters(GpuObjects.AdapterMemory));
        Assert.Contains("Total Committed", GpuCounterCatalog.Counters(GpuObjects.ProcessMemory));
        Assert.Contains("Local Usage", GpuCounterCatalog.Counters(GpuObjects.LocalAdapterMemory));
        Assert.Contains("Non Local Usage", GpuCounterCatalog.Counters(GpuObjects.NonLocalAdapterMemory));
    }

    [Fact]
    public void PG01_001_live_does_not_fake_known_when_missing()
    {
        var missing = new ScriptedInventory { Present = false };
        Assert.False(GpuCounterCatalog.CategoryPresent(GpuObjects.Engine, missing));
        Assert.False(GpuCounterCatalog.HasCounter(GpuObjects.Engine, "Utilization Percentage", "", missing));
        Assert.Empty(GpuCounterCatalog.LiveCounters(GpuObjects.Engine, "", 256, missing));
        Assert.False(GpuCounterCatalog.Snapshot(GpuObjects.Engine, inventory: missing).CategoryPresent);
    }

    [Fact]
    public void PG01_001_check_and_snapshot_use_live_inventory()
    {
        var live = new ScriptedInventory
        {
            Present = true,
            Counters = ["Utilization Percentage", "Running Time"],
            Instances = ["pid_1000_luid_0x0_0x1_eng_0_engtype_3D"]
        };
        Assert.True(GpuCounterCatalog.CategoryPresent(GpuObjects.Engine, live));
        Assert.True(GpuCounterCatalog.HasCounter(GpuObjects.Engine, "Utilization Percentage", "", live));
        var snap = GpuCounterCatalog.Snapshot(GpuObjects.Engine, inventory: live);
        Assert.Equal(["Utilization Percentage", "Running Time"], snap.Counters);
        Assert.Equal(["pid_1000_luid_0x0_0x1_eng_0_engtype_3D"], snap.Instances);
    }

    [Fact]
    public async Task PG01_001_watch_subscribes_to_live_counters_and_instances()
    {
        var inventory = new ScriptedInventory
        {
            Present = true,
            Counters = ["Dedicated Usage"],
            Instances = ["luid_0x0_0x1_phys_0"]
        };
        var hits = new List<CatalogSnapshot>();
        await GpuCounterCatalog.WatchAsync(
            new CatalogWatchOptions
            {
                Category = GpuObjects.AdapterMemory,
                Count = 2,
                Interval = TimeSpan.FromSeconds(1),
                EmitOnlyOnChange = false,
                Inventory = inventory,
                Clock = new ImmediateClock()
            },
            hits.Add);

        Assert.Equal(2, hits.Count);
        Assert.All(hits, s => Assert.Equal(["Dedicated Usage"], s.Counters));
    }

    [Fact]
    public void PG01_002_does_not_invent_total()
    {
        Assert.Equal("", GpuPaths.InstanceOrNone(null));
        Assert.Equal("", GpuPaths.InstanceOrNone(""));
        Assert.Equal("", GpuPaths.InstanceOrNone("   "));
        var paths = GpuPaths.Short();
        Assert.All(paths, p => Assert.Equal("", p.Instance));
        Assert.DoesNotContain(paths, p => p.Instance.Equals("_Total", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PG01_002_named_instance_is_kept()
    {
        const string name = "pid_1000_luid_0x0_0x1_eng_0_engtype_3D";
        Assert.Equal(name, GpuPaths.InstanceOrNone(" " + name + " "));
        var paths = GpuPaths.Short(name);
        Assert.All(paths, p => Assert.Equal(name, p.Instance));
    }

    [Fact]
    public void PG01_002_short_job_objects()
    {
        var paths = GpuPaths.Short("luid_0x0_0x1_phys_0");
        Assert.Equal(2, paths.Count);
        Assert.Equal(GpuObjects.Engine, paths[0].Category);
        Assert.Equal(GpuPaths.Utilization, paths[0].Counter);
        Assert.Equal("%", paths[0].Unit);
        Assert.Equal(GpuObjects.AdapterMemory, paths[1].Category);
        Assert.Equal(GpuPaths.Dedicated, paths[1].Counter);
        Assert.Equal("B", paths[1].Unit);
        Assert.DoesNotContain(paths, p => p.Category == GpuObjects.ProcessMemory);
    }

    private sealed class ScriptedInventory : ICounterInventory
    {
        public bool Present { get; set; }
        public IReadOnlyList<string> Counters { get; set; } = [];
        public IReadOnlyList<string> Instances { get; set; } = [];

        public bool CategoryPresent(string category) => Present;

        public bool InstancePresent(string category, string instance)
            => Present && Instances.Contains(instance, StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<string> LiveCounters(string category, string instance, int cap)
            => Present ? Counters.Take(cap).ToArray() : [];

        public IReadOnlyList<string> LiveInstances(string category, int cap)
            => Present ? Instances.Take(cap).ToArray() : [];
    }

    private sealed class ImmediateClock : TimeProvider
    {
        private DateTimeOffset _utc = new(2026, 9, 28, 1, 0, 0, TimeSpan.Zero);

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
