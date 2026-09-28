using Vestigium.Helpers.PerfMon;
using Vestigium.Helpers.PerfMon.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PerfMonNetworkPn01Tests
{
    [Fact]
    public void PN01_001_catalog_lists_network_objects()
    {
        Assert.Equal(63, NetworkObjects.All.Count);
        Assert.Equal(NetworkObjects.All, NetworkCounterCatalog.Categories);
        Assert.True(NetworkCounterCatalog.IsKnownCategory("network interface"));
        Assert.True(NetworkCounterCatalog.IsKnownCategory(NetworkObjects.WinNatUdp));
        Assert.True(NetworkCounterCatalog.IsKnownCategory(NetworkObjects.TcpipPerformanceDiagnosticsPerCpu));
        Assert.False(NetworkCounterCatalog.IsKnownCategory("PhysicalDisk"));
        Assert.Throws<ArgumentException>(() => NetworkCounterCatalog.Counters("PhysicalDisk"));
    }

    [Fact]
    public void PN01_001_known_rates_cover_interface()
    {
        Assert.Contains("Bytes Total/sec", NetworkCounterCatalog.Counters(NetworkObjects.NetworkInterface));
        Assert.Contains("Packets Received Errors", NetworkCounterCatalog.Counters(NetworkObjects.NetworkInterface));
        Assert.Contains("Output Queue Length", NetworkCounterCatalog.Counters(NetworkObjects.NetworkAdapter));
        Assert.Empty(NetworkCounterCatalog.Counters(NetworkObjects.WinNat));
    }

    [Fact]
    public void PN01_001_live_does_not_fake_known_when_missing()
    {
        var missing = new ScriptedInventory { Present = false };
        Assert.False(NetworkCounterCatalog.CategoryPresent(NetworkObjects.NetworkInterface, missing));
        Assert.Empty(NetworkCounterCatalog.LiveCounters(NetworkObjects.NetworkInterface, "_Total", 256, missing));
        Assert.False(NetworkCounterCatalog.Snapshot(NetworkObjects.NetworkInterface, inventory: missing).CategoryPresent);
    }

    [Fact]
    public void PN01_001_check_and_snapshot_use_live_inventory()
    {
        var live = new ScriptedInventory
        {
            Present = true,
            Counters = ["Bytes Total/sec", "Output Queue Length"],
            Instances = ["_Total", "Ethernet"]
        };
        Assert.True(NetworkCounterCatalog.HasCounter(NetworkObjects.NetworkInterface, "Bytes Total/sec", "_Total", live));
        var snap = NetworkCounterCatalog.Snapshot(NetworkObjects.NetworkInterface, inventory: live);
        Assert.Equal(["Bytes Total/sec", "Output Queue Length"], snap.Counters);
        Assert.Equal(["_Total", "Ethernet"], snap.Instances);
    }

    [Fact]
    public async Task PN01_001_watch_subscribes()
    {
        var inventory = new ScriptedInventory
        {
            Present = true,
            Counters = ["Bytes Total/sec"],
            Instances = ["_Total"]
        };
        var hits = new List<CatalogSnapshot>();
        await NetworkCounterCatalog.WatchAsync(
            new CatalogWatchOptions
            {
                Category = NetworkObjects.NetworkInterface,
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
            => Present && Instances.Contains(instance, StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<string> LiveCounters(string category, string instance, int cap)
            => Present ? Counters.Take(cap).ToArray() : [];

        public IReadOnlyList<string> LiveInstances(string category, int cap)
            => Present ? Instances.Take(cap).ToArray() : [];
    }

    private sealed class ImmediateClock : TimeProvider
    {
        private DateTimeOffset _utc = new(2026, 9, 28, 20, 40, 0, TimeSpan.Zero);

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
