using System.Diagnostics;
using System.Text.Json;
using Vestigium.Helpers.PerfMon;
using Vestigium.Helpers.PerfMon.Disk;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
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

    [Fact]
    public void PD01_002_empty_instance_becomes_total()
    {
        Assert.Equal("_Total", DiskPaths.InstanceOrTotal(""));
        Assert.Equal("_Total", DiskPaths.InstanceOrTotal("   "));
        Assert.Equal("_Total", DiskPaths.InstanceOrTotal(null));
        var paths = DiskPaths.Physical("  ");
        Assert.All(paths, p => Assert.Equal("_Total", p.Instance));
        Assert.Equal(DiskPaths.PhysicalShort.Length, paths.Count);
    }

    [Fact]
    public void PD01_002_named_instance_is_kept()
    {
        Assert.Equal("0 C:", DiskPaths.InstanceOrTotal(" 0 C: "));
        var paths = DiskPaths.Physical("0 C:");
        Assert.All(paths, p => Assert.Equal("0 C:", p.Instance));
    }

    [Fact]
    public void PD01_002_paths_are_physical_disk()
    {
        var paths = DiskPaths.Physical();
        Assert.All(paths, p => Assert.Equal(DiskObjects.PhysicalDisk, p.Category));
        Assert.DoesNotContain(paths, p => p.Category == DiskObjects.LogicalDisk);
        Assert.Equal(DiskPaths.PhysicalShort, paths.Select(p => p.Counter));
        Assert.Equal("/sec", paths.First(p => p.Counter == "Disk Bytes/sec").Unit);
        Assert.Equal("s", paths.First(p => p.Counter == "Avg. Disk sec/Read").Unit);
        Assert.Equal("count", paths.First(p => p.Counter == "Current Disk Queue Length").Unit);
        Assert.Equal("%", paths.First(p => p.Counter == "% Disk Time").Unit);
    }

    [Fact]
    public void PD01_003_disks_respect_cap()
    {
        var names = new List<string> { "_Total" };
        names.AddRange(Enumerable.Range(0, 10).Select(i => $"{i} X:"));
        var inv = new ScriptedInventory { Present = true, Instances = names };
        var paths = DiskPaths.For(new DiskSampleOptions
        {
            IncludeDisks = true,
            InstanceCap = 3,
            Inventory = inv
        });
        var disks = paths
            .Where(p => p.Counter == "Disk Bytes/sec")
            .Select(p => p.Instance)
            .ToArray();
        Assert.Equal(["_Total", "0 X:", "1 X:", "2 X:"], disks);
        Assert.Equal(3, DiskPaths.Disks(inv, 3).Count);
        Assert.Empty(DiskPaths.Disks(inv, 0));
    }

    [Fact]
    public void PD01_003_disks_omit_total()
    {
        var inv = new ScriptedInventory
        {
            Present = true,
            Instances = ["_Total", "_total", "0 C:", "1 D:"]
        };
        var disks = DiskPaths.Disks(inv, 256);
        Assert.Equal(["0 C:", "1 D:"], disks);
        Assert.DoesNotContain(disks, n => n.Equals("_Total", StringComparison.OrdinalIgnoreCase));

        var paths = DiskPaths.For(new DiskSampleOptions { IncludeDisks = true, Inventory = inv });
        Assert.Equal(1, paths.Count(p => p.Instance == "_Total" && p.Counter == "Disk Bytes/sec"));
        Assert.Contains(paths, p => p.Instance == "0 C:" && p.Counter == "Avg. Disk sec/Read");
        Assert.All(paths, p => Assert.Equal(DiskObjects.PhysicalDisk, p.Category));
    }

    [Fact]
    public void PD01_004_missing_object_stays_physical()
    {
        var onlyLogical = new ScriptedInventory
        {
            Present = true,
            Counters = ["% Free Space", "Disk Bytes/sec"],
            Instances = ["C:"]
        };
        var paths = DiskPaths.For(new DiskSampleOptions
        {
            IncludeDisks = true,
            Inventory = onlyLogical
        });
        Assert.Equal(DiskObjects.PhysicalDisk, DiskPaths.ObjectName);
        Assert.All(paths, p => Assert.Equal(DiskObjects.PhysicalDisk, p.Category));
        Assert.DoesNotContain(paths, p => p.Category == DiskObjects.LogicalDisk);
        Assert.DoesNotContain(paths, p => p.Counter == "% Free Space");
        Assert.False(DiskCounterCatalog.CategoryPresent(DiskObjects.PhysicalDisk, new ScriptedInventory { Present = false }));
    }

    [Fact]
    public async Task PD01_005_run_uses_shared_job()
    {
        var paths = DiskPaths.Physical("_Total");
        var fake = new FakeCounterSource();
        foreach (var path in paths)
            fake.Seed(path, SampleRecord.Ok(path, 12));

        var result = await DiskPerf.RunAsync(new DiskSampleOptions
        {
            Count = 1,
            Source = fake,
            Clock = new ImmediateClock()
        });

        Assert.Equal(SampleStatus.Ok, result.Status);
        Assert.Equal(paths.Count, result.Samples.Count);
        Assert.All(result.Samples, s =>
        {
            Assert.Equal(DiskObjects.PhysicalDisk, s.Category);
            Assert.Equal(12, s.Value);
        });
    }

    [Fact]
    public void PD01_005_live_total()
    {
        try
        {
            if (!PerformanceCounterCategory.Exists(DiskObjects.PhysicalDisk))
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

        var path = new CounterPath(DiskObjects.PhysicalDisk, "Disk Bytes/sec", "_Total", "/sec");
        var source = new PerformanceCounterSource();
        _ = source.Read(path);
        var row = source.Read(path);
        if (row.Status != SampleStatus.Ok)
            return;
        Assert.NotNull(row.Value);
    }

    [Fact]
    public void PD01_006_register_is_host_only()
    {
        var register = typeof(DiskPerfCatalog).GetMethod("Register");
        Assert.NotNull(register);
        Assert.Equal(typeof(VestigiumLoggerOptions), register!.GetParameters()[0].ParameterType);
        Assert.Null(typeof(DiskPerfCatalog).GetMethod("Initialize"));

        VestigiumLogger.Shutdown();
        Assert.False(VestigiumLogger.IsInitialized);
        Assert.Null(Record.Exception(() =>
            DiskPerfLog.Information(
                DiskPerfEvents.ProbeComplete,
                VestigiumStatus.Success,
                DiskPerfCatalog.Subcategories.Probe,
                "noop")));
        Assert.False(VestigiumLogger.IsInitialized);
    }

    [Fact]
    public void PD01_006_writes_noop_until_host_starts()
    {
        VestigiumLogger.Shutdown();
        var fake = new FakeCounterSource();
        foreach (var path in DiskPaths.Physical("_Total"))
            fake.Seed(path, SampleRecord.Ok(path, 1));

        var ex = Record.Exception(() => DiskPerf.RunAsync(new DiskSampleOptions
        {
            Count = 1,
            Source = fake,
            Clock = new ImmediateClock()
        }).GetAwaiter().GetResult());
        Assert.Null(ex);
        Assert.False(VestigiumLogger.IsInitialized);
    }

    [Fact]
    public void PD01_007_catalog_matches_constants()
    {
        var jsonPath = Path.Combine(
            Path.GetDirectoryName(typeof(DiskPerfCatalog).Assembly.Location)!,
            "EventCatalog",
            "disk.json");
        Assert.True(File.Exists(jsonPath), jsonPath);

        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var root = doc.RootElement;
        Assert.Equal(DiskPerfCatalog.AppId, root.GetProperty("app").GetString());
        Assert.Equal(DiskPerfCatalog.Category, root.GetProperty("category").GetString());
        Assert.Equal(DiskPerfEvents.BlockStart, root.GetProperty("block").GetProperty("start").GetInt32());
        Assert.Equal(DiskPerfEvents.BlockEnd, root.GetProperty("block").GetProperty("end").GetInt32());

        var events = root.GetProperty("events").EnumerateArray().ToArray();
        Assert.Equal(DiskPerfCatalog.Rows.Length, events.Length);

        var constants = typeof(DiskPerfEvents)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(int) && f.Name is not ("BlockStart" or "BlockEnd"))
            .ToDictionary(f => f.Name, f => (int)f.GetValue(null)!);

        foreach (var (row, node) in DiskPerfCatalog.Rows.Zip(events))
        {
            Assert.Equal(row.EventId, node.GetProperty("eventId").GetInt32());
            Assert.Equal(row.Name, node.GetProperty("name").GetString());
            Assert.Equal(row.Severity, node.GetProperty("severity").GetString());
            Assert.Equal(row.Subcategory, node.GetProperty("subcategory").GetString());
            Assert.Equal(0, row.EventId % 5);
            Assert.InRange(row.EventId, DiskPerfEvents.BlockStart, DiskPerfEvents.BlockEnd);
            Assert.Equal(constants[row.Name], row.EventId);
        }
    }

    [Fact]
    public void PD01_008_probe_references_shared_only()
    {
        var csproj = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(typeof(DiskPerf).Assembly.Location)!,
            "..", "..", "..", "..", "..",
            "Vestigium.Helpers.PerfMon.Disk",
            "Vestigium.Helpers.PerfMon.Disk.csproj"));
        if (!File.Exists(csproj))
        {
            csproj = FindCsproj();
        }
        var text = File.ReadAllText(csproj);
        Assert.Contains("Vestigium.Helpers.PerfMon\\Vestigium.Helpers.PerfMon.csproj", text);
        Assert.DoesNotContain("Vestigium.Helpers.Charts", text);
        Assert.DoesNotContain("Vestigium.Helpers.Analytics", text);
        Assert.DoesNotContain("Vestigium.Helpers.FileIo", text);
        Assert.DoesNotContain("Vestigium.Helpers.Processes", text);
        Assert.DoesNotContain("System.Diagnostics.PerformanceCounter", text);

        var names = typeof(DiskSampleOptions).GetProperties().Select(p => p.Name);
        Assert.DoesNotContain(names, n => n.Contains("File", StringComparison.OrdinalIgnoreCase) && n.Contains("Copy", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(DiskPerf).GetMethods().Select(m => m.Name), n => n.Contains("Copy", StringComparison.OrdinalIgnoreCase));
    }

    private static string FindCsproj()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var hit = dir.GetFiles("Vestigium.Helpers.PerfMon.Disk.csproj", SearchOption.AllDirectories).FirstOrDefault();
            if (hit is not null)
                return hit.FullName;
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Vestigium.Helpers.PerfMon.Disk.csproj");
    }

    [Fact]
    public void PR02b_004_disk_types_agree_with_shard()
    {
        PdhCatalogAgreement.AssertMatches(
            typeof(DiskPerfCatalog).Assembly,
            PhysicalDisk.Category,
            PhysicalDisk.Counters,
            DiskCounterCatalog.Counters(PhysicalDisk.Category));
        PdhCatalogAgreement.AssertMatches(
            typeof(DiskPerfCatalog).Assembly,
            LogicalDisk.Category,
            LogicalDisk.Counters,
            DiskCounterCatalog.Counters(LogicalDisk.Category));
        Assert.Equal(PhysicalDisk.DiskBytesPerSec, DiskPaths.PhysicalShort[0]);
        Assert.DoesNotContain(DiskPaths.Physical(), p => p.Category == LogicalDisk.Category);
        Assert.Null(typeof(PhysicalDisk).Assembly.GetType("Vestigium.Helpers.PerfMon.Disk.ReFS"));
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
