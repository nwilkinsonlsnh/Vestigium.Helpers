using System.Diagnostics;
using System.Text.Json;
using Vestigium.Helpers.PerfMon;
using Vestigium.Helpers.PerfMon.Gpu;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
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

    [Fact]
    public void PG01_003_instances_respect_cap()
    {
        var names = Enumerable.Range(0, 10).Select(i => $"luid_0x0_0x{i}_phys_0").ToArray();
        var inv = new ScriptedInventory { Present = true, Instances = names };
        var live = GpuPaths.Instances(GpuObjects.AdapterMemory, inv, 3);
        Assert.Equal(names.Take(3), live);
        Assert.Empty(GpuPaths.Instances(GpuObjects.AdapterMemory, inv, 0));

        var paths = GpuPaths.For(new GpuSampleOptions
        {
            IncludeAllInstances = true,
            InstanceCap = 2,
            Inventory = inv
        });
        Assert.Equal(2, paths.Count(p => p.Category == GpuObjects.Engine));
        Assert.Equal(2, paths.Count(p => p.Category == GpuObjects.AdapterMemory));
        Assert.DoesNotContain(paths, p => p.Instance.Equals("_Total", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PG01_003_named_plus_live_keeps_name()
    {
        var inv = new ScriptedInventory
        {
            Present = true,
            Instances = ["luid_0x0_0x1_phys_0", "luid_0x0_0x2_phys_0"]
        };
        const string named = "pid_9_luid_0x0_0x9_eng_0_engtype_3D";
        var paths = GpuPaths.For(new GpuSampleOptions
        {
            Instance = named,
            IncludeAllInstances = true,
            InstanceCap = 8,
            Inventory = inv
        });
        Assert.Contains(paths, p => p.Category == GpuObjects.Engine && p.Instance == named);
        Assert.Contains(paths, p => p.Instance == "luid_0x0_0x1_phys_0");
    }

    [Fact]
    public void PG01_004_missing_object_is_omitted()
    {
        var adapterOnly = new ScriptedInventory
        {
            Present = true,
            Only = [GpuObjects.AdapterMemory],
            Instances = ["luid_0x0_0x1_phys_0"]
        };
        var paths = GpuPaths.For(new GpuSampleOptions
        {
            IncludeAllInstances = true,
            Inventory = adapterOnly
        });
        Assert.DoesNotContain(paths, p => p.Category == GpuObjects.Engine);
        Assert.All(paths, p => Assert.Equal(GpuObjects.AdapterMemory, p.Category));
    }

    [Fact]
    public void PG01_004_both_missing_is_empty()
    {
        var none = new ScriptedInventory { Present = false };
        var paths = GpuPaths.For(new GpuSampleOptions
        {
            IncludeAllInstances = true,
            Inventory = none
        });
        Assert.Empty(paths);
        Assert.Null(typeof(GpuCounterCatalog).Assembly.GetType("Nvidia.Nvml.Nvml"));
        Assert.Null(typeof(GpuSampleOptions).GetProperty("DxgiFactory"));
    }

    [Fact]
    public async Task PG01_005_run_uses_shared_job()
    {
        const string inst = "luid_0x0_0x1_phys_0";
        var paths = GpuPaths.Short(inst);
        var fake = new FakeCounterSource();
        foreach (var path in paths)
            fake.Seed(path, SampleRecord.Ok(path, 12));

        var result = await GpuPerf.RunAsync(new GpuSampleOptions
        {
            Instance = inst,
            IncludeAllInstances = false,
            Count = 1,
            Source = fake,
            Clock = new ImmediateClock()
        });

        Assert.Equal(SampleStatus.Ok, result.Status);
        Assert.Equal(paths.Count, result.Samples.Count);
        Assert.All(result.Samples, s => Assert.Equal(12, s.Value));
    }

    [Fact]
    public void PG01_005_headless_rejects_empty_paths()
    {
        var none = new ScriptedInventory { Present = false };
        var ex = Assert.Throws<ArgumentException>(() => GpuPerf.RunAsync(new GpuSampleOptions
        {
            IncludeAllInstances = true,
            Inventory = none,
            Count = 1,
            Clock = new ImmediateClock()
        }).GetAwaiter().GetResult());
        Assert.Contains("Path list cannot be empty", ex.Message);
    }

    [Fact]
    public void PG01_005_live_engine()
    {
        try
        {
            if (!PerformanceCounterCategory.Exists(GpuObjects.Engine)
                && !PerformanceCounterCategory.Exists(GpuObjects.AdapterMemory))
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
    }

    [Fact]
    public void PG01_006_register_is_host_only()
    {
        var register = typeof(GpuPerfCatalog).GetMethod("Register");
        Assert.NotNull(register);
        Assert.Equal(typeof(VestigiumLoggerOptions), register!.GetParameters()[0].ParameterType);
        Assert.Null(typeof(GpuPerfCatalog).GetMethod("Initialize"));

        VestigiumLogger.Shutdown();
        Assert.False(VestigiumLogger.IsInitialized);
        Assert.Null(Record.Exception(() =>
            GpuPerfLog.Information(
                GpuPerfEvents.ProbeComplete,
                VestigiumStatus.Success,
                GpuPerfCatalog.Subcategories.Probe,
                "noop")));
        Assert.False(VestigiumLogger.IsInitialized);
    }

    [Fact]
    public void PG01_006_writes_noop_until_host_starts()
    {
        VestigiumLogger.Shutdown();
        var fake = new FakeCounterSource();
        foreach (var path in GpuPaths.Short("luid_0x0_0x1_phys_0"))
            fake.Seed(path, SampleRecord.Ok(path, 1));

        var ex = Record.Exception(() => GpuPerf.RunAsync(new GpuSampleOptions
        {
            Instance = "luid_0x0_0x1_phys_0",
            IncludeAllInstances = false,
            Count = 1,
            Source = fake,
            Clock = new ImmediateClock()
        }).GetAwaiter().GetResult());
        Assert.Null(ex);
        Assert.False(VestigiumLogger.IsInitialized);
    }

    [Fact]
    public void PG01_007_catalog_matches_constants()
    {
        var jsonPath = Path.Combine(
            Path.GetDirectoryName(typeof(GpuPerfCatalog).Assembly.Location)!,
            "EventCatalog",
            "gpu.json");
        Assert.True(File.Exists(jsonPath), jsonPath);

        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var root = doc.RootElement;
        Assert.Equal(GpuPerfCatalog.AppId, root.GetProperty("app").GetString());
        Assert.Equal(GpuPerfCatalog.Category, root.GetProperty("category").GetString());
        Assert.Equal(GpuPerfEvents.BlockStart, root.GetProperty("block").GetProperty("start").GetInt32());
        Assert.Equal(GpuPerfEvents.BlockEnd, root.GetProperty("block").GetProperty("end").GetInt32());

        var events = root.GetProperty("events").EnumerateArray().ToArray();
        Assert.Equal(GpuPerfCatalog.Rows.Length, events.Length);

        var constants = typeof(GpuPerfEvents)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(int) && f.Name is not ("BlockStart" or "BlockEnd"))
            .ToDictionary(f => f.Name, f => (int)f.GetValue(null)!);

        foreach (var (row, node) in GpuPerfCatalog.Rows.Zip(events))
        {
            Assert.Equal(row.EventId, node.GetProperty("eventId").GetInt32());
            Assert.Equal(row.Name, node.GetProperty("name").GetString());
            Assert.Equal(row.Severity, node.GetProperty("severity").GetString());
            Assert.Equal(row.Subcategory, node.GetProperty("subcategory").GetString());
            Assert.Equal(0, row.EventId % 5);
            Assert.InRange(row.EventId, GpuPerfEvents.BlockStart, GpuPerfEvents.BlockEnd);
            Assert.Equal(constants[row.Name], row.EventId);
        }
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
