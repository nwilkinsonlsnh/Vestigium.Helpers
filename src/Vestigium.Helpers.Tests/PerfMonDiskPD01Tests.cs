using Vestigium.Helpers.PerfMon;
using Vestigium.Helpers.PerfMon.Disk;

namespace Vestigium.Helpers.Tests;

public sealed class PerfMonDiskPd01Tests
{
    [Fact]
    public void PD01_001_catalog_lists_disk_objects()
    {
        Assert.Equal(19, DiskCounterCatalog.Categories.Count);
        Assert.Equal(DiskObjects.All, DiskCounterCatalog.Categories);
        Assert.True(DiskCounterCatalog.IsKnownCategory("physicaldisk"));
        Assert.True(DiskCounterCatalog.IsKnownCategory(DiskObjects.StorportUnitQueue));
        Assert.True(DiskCounterCatalog.IsKnownCategory(DiskObjects.VhdBucketizedPerformance));
        Assert.False(DiskCounterCatalog.IsKnownCategory("Processor"));
        Assert.Throws<ArgumentException>(() => DiskCounterCatalog.Counters("Processor"));
    }

    [Fact]
    public void PD01_001_known_rates_cover_physical_and_logical()
    {
        Assert.Contains("Disk Bytes/sec", DiskCounterCatalog.Counters(DiskObjects.PhysicalDisk));
        Assert.Contains("Avg. Disk sec/Read", DiskCounterCatalog.Counters(DiskObjects.PhysicalDisk));
        Assert.Contains("Current Disk Queue Length", DiskCounterCatalog.Counters(DiskObjects.PhysicalDisk));
        Assert.Contains("% Free Space", DiskCounterCatalog.Counters(DiskObjects.LogicalDisk));
        Assert.Contains("Free Megabytes", DiskCounterCatalog.Counters(DiskObjects.LogicalDisk));
        Assert.Empty(DiskCounterCatalog.Counters(DiskObjects.ReFS));
    }

    [Fact]
    public void PD01_001_live_does_not_fake_known_when_missing()
    {
        var missing = new ScriptedInventory { Present = false };
        Assert.False(DiskCounterCatalog.CategoryPresent(DiskObjects.PhysicalDisk, missing));
        Assert.False(DiskCounterCatalog.HasCounter(DiskObjects.PhysicalDisk, "Disk Bytes/sec", "_Total", missing));
        Assert.Empty(DiskCounterCatalog.LiveCounters(DiskObjects.PhysicalDisk, "_Total", 256, missing));
        Assert.False(DiskCounterCatalog.Snapshot(DiskObjects.PhysicalDisk, inventory: missing).CategoryPresent);
    }

    [Fact]
    public void PD01_001_check_and_snapshot_use_live_inventory()
    {
        var live = new ScriptedInventory
        {
            Present = true,
            Counters = ["Disk Bytes/sec", "Current Disk Queue Length"],
            Instances = ["_Total", "0 C:"]
        };
        Assert.True(DiskCounterCatalog.CategoryPresent(DiskObjects.PhysicalDisk, live));
        Assert.True(DiskCounterCatalog.HasCounter(DiskObjects.PhysicalDisk, "Disk Bytes/sec", "_Total", live));
        Assert.True(DiskCounterCatalog.HasInstance(DiskObjects.PhysicalDisk, "0 C:", live));
        var snap = DiskCounterCatalog.Snapshot(DiskObjects.PhysicalDisk, inventory: live);
        Assert.Equal(["Disk Bytes/sec", "Current Disk Queue Length"], snap.Counters);
        Assert.Equal(["_Total", "0 C:"], snap.Instances);
    }

    [Fact]
    public async Task PD01_001_watch_subscribes_to_live_counters_and_instances()
    {
        var inventory = new ScriptedInventory
        {
            Present = true,
            Counters = ["Disk Bytes/sec"],
            Instances = ["_Total"]
        };
        var hits = new List<CatalogSnapshot>();
        await DiskCounterCatalog.WatchAsync(
            new CatalogWatchOptions
            {
                Category = DiskObjects.LogicalDisk,
                Count = 2,
                Interval = TimeSpan.FromSeconds(1),
                EmitOnlyOnChange = false,
                Inventory = inventory,
                Clock = new ImmediateClock()
            },
            hits.Add);

        Assert.Equal(2, hits.Count);
        Assert.All(hits, s => Assert.Equal(["Disk Bytes/sec"], s.Counters));
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
        private DateTimeOffset _utc = new(2026, 9, 27, 23, 0, 0, TimeSpan.Zero);

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
