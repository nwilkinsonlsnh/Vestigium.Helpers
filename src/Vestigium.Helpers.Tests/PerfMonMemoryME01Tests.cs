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
