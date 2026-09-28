using Vestigium.Helpers.PerfMon;
using Vestigium.Helpers.PerfMon.PageFile;

namespace Vestigium.Helpers.Tests;

public sealed class PerfMonPageFilePf01Tests
{
    [Fact]
    public void PF01_001_catalog_lists_paging_file()
    {
        Assert.Equal(PageFileObjects.All, PageFileCounterCatalog.Categories);
        Assert.True(PageFileCounterCatalog.IsKnownCategory("paging file"));
        Assert.False(PageFileCounterCatalog.IsKnownCategory("Memory"));
        Assert.Throws<ArgumentException>(() => PageFileCounterCatalog.Counters("Memory"));
    }

    [Fact]
    public void PF01_001_known_counters_are_usage()
    {
        Assert.Equal(["% Usage", "% Usage Peak"], PageFileCounterCatalog.Counters(PageFileObjects.PagingFile));
    }

    [Fact]
    public void PF01_001_live_does_not_fake_known_when_missing()
    {
        var missing = new ScriptedInventory { Present = false };
        Assert.False(PageFileCounterCatalog.CategoryPresent(PageFileObjects.PagingFile, missing));
        Assert.Empty(PageFileCounterCatalog.LiveCounters(PageFileObjects.PagingFile, "_Total", 256, missing));
        Assert.False(PageFileCounterCatalog.Snapshot(PageFileObjects.PagingFile, inventory: missing).CategoryPresent);
    }

    [Fact]
    public void PF01_001_check_and_snapshot_use_live_inventory()
    {
        var live = new ScriptedInventory
        {
            Present = true,
            Counters = ["% Usage", "% Usage Peak"],
            Instances = ["_Total", @"C:\pagefile.sys"]
        };
        Assert.True(PageFileCounterCatalog.HasCounter(PageFileObjects.PagingFile, "% Usage", "_Total", live));
        var snap = PageFileCounterCatalog.Snapshot(PageFileObjects.PagingFile, inventory: live);
        Assert.Equal(["% Usage", "% Usage Peak"], snap.Counters);
        Assert.Equal(["_Total", @"C:\pagefile.sys"], snap.Instances);
    }

    [Fact]
    public async Task PF01_001_watch_subscribes()
    {
        var inventory = new ScriptedInventory
        {
            Present = true,
            Counters = ["% Usage"],
            Instances = ["_Total"]
        };
        var hits = new List<CatalogSnapshot>();
        await PageFileCounterCatalog.WatchAsync(
            new CatalogWatchOptions
            {
                Category = PageFileObjects.PagingFile,
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
    public void PF01_002_empty_instance_becomes_total()
    {
        Assert.Equal("_Total", PageFilePaths.InstanceOrTotal(""));
        Assert.Equal("_Total", PageFilePaths.InstanceOrTotal("   "));
        Assert.Equal("_Total", PageFilePaths.InstanceOrTotal(null));
        var paths = PageFilePaths.Usage("  ");
        Assert.All(paths, p => Assert.Equal("_Total", p.Instance));
        Assert.Equal(2, paths.Count);
    }

    [Fact]
    public void PF01_002_named_instance_is_kept()
    {
        const string file = @"C:\pagefile.sys";
        Assert.Equal(file, PageFilePaths.InstanceOrTotal(" " + file + " "));
        var paths = PageFilePaths.Usage(file);
        Assert.All(paths, p => Assert.Equal(file, p.Instance));
    }

    [Fact]
    public void PF01_002_paths_are_paging_file()
    {
        var paths = PageFilePaths.Usage();
        Assert.All(paths, p => Assert.Equal(PageFileObjects.PagingFile, p.Category));
        Assert.Equal(PageFilePaths.ShortCounters, paths.Select(p => p.Counter));
        Assert.All(paths, p => Assert.Equal("%", p.Unit));
        Assert.DoesNotContain(paths, p => p.Category == "Memory");
        Assert.DoesNotContain(paths, p => p.Counter.Contains("Pages/sec", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PF01_003_files_respect_cap()
    {
        var names = new List<string> { "_Total", @"C:\pagefile.sys", @"D:\pagefile.sys", @"E:\pagefile.sys" };
        var inv = new ScriptedInventory { Present = true, Instances = names };
        var paths = PageFilePaths.For(new PageFileSampleOptions
        {
            IncludeFiles = true,
            InstanceCap = 2,
            Inventory = inv
        });
        var files = paths.Where(p => p.Counter == "% Usage").Select(p => p.Instance).ToArray();
        Assert.Equal(["_Total", @"C:\pagefile.sys", @"D:\pagefile.sys"], files);
        Assert.Equal(2, PageFilePaths.Files(inv, 2).Count);
        Assert.Empty(PageFilePaths.Files(inv, 0));
    }

    [Fact]
    public void PF01_003_files_omit_total()
    {
        var inv = new ScriptedInventory
        {
            Present = true,
            Instances = ["_Total", "_total", @"C:\pagefile.sys"]
        };
        Assert.Equal([@"C:\pagefile.sys"], PageFilePaths.Files(inv, 256));
        var paths = PageFilePaths.For(new PageFileSampleOptions { IncludeFiles = true, Inventory = inv });
        Assert.Equal(1, paths.Count(p => p.Instance == "_Total" && p.Counter == "% Usage"));
        Assert.All(paths, p => Assert.Equal(PageFileObjects.PagingFile, p.Category));
    }

    [Fact]
    public void PF01_004_missing_object_stays_paging_file()
    {
        var missing = new ScriptedInventory { Present = false };
        var paths = PageFilePaths.For(new PageFileSampleOptions
        {
            IncludeFiles = true,
            Inventory = missing
        });
        Assert.Equal(PageFileObjects.PagingFile, PageFilePaths.ObjectName);
        Assert.All(paths, p => Assert.Equal(PageFileObjects.PagingFile, p.Category));
        Assert.DoesNotContain(paths, p => p.Category == "Memory");
        Assert.DoesNotContain(paths, p => p.Counter.Contains("Pages/sec", StringComparison.OrdinalIgnoreCase));
        Assert.False(PageFileCounterCatalog.CategoryPresent(PageFileObjects.PagingFile, missing));
        Assert.Null(typeof(PageFileSampleOptions).GetProperty("IncludeMemoryRates"));
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
        private DateTimeOffset _utc = new(2026, 9, 28, 21, 20, 0, TimeSpan.Zero);

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
