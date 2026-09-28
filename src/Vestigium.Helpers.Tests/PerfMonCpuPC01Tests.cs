using System.Diagnostics;
using System.Text.Json;
using Vestigium.Helpers.PerfMon;
using Vestigium.Helpers.PerfMon.Cpu;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
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

    [Fact]
    public void PC01_003_cores_respect_cap()
    {
        var names = new List<string> { "_Total" };
        names.AddRange(Enumerable.Range(0, 10).Select(i => $"0,{i}"));
        var inv = new ScriptedInventory
        {
            Present = true,
            Only = [CpuObjects.ProcessorInformation],
            Instances = names
        };

        var paths = CpuPaths.For(new CpuSampleOptions
        {
            IncludeCores = true,
            InstanceCap = 3,
            Inventory = inv
        }, inv);

        var cores = paths
            .Where(p => p.Counter == "% Processor Time")
            .Select(p => p.Instance)
            .ToArray();
        Assert.Equal(["_Total", "0,0", "0,1", "0,2"], cores);
        Assert.Equal(3, CpuPaths.Cores(inv, 3).Count);
        Assert.Empty(CpuPaths.Cores(inv, 0));
        Assert.Empty(CpuPaths.For(new CpuSampleOptions { IncludeCores = true, InstanceCap = 0, Inventory = inv }, inv)
            .Where(p => p.Instance != "_Total" && p.Counter != CpuPaths.QueueLength));
    }

    [Fact]
    public void PC01_003_cores_omit_total()
    {
        var inv = new ScriptedInventory
        {
            Present = true,
            Only = [CpuObjects.Processor],
            Instances = ["_Total", "_total", "0", "1"]
        };
        var cores = CpuPaths.Cores(inv, 256);
        Assert.Equal(["0", "1"], cores);
        Assert.DoesNotContain(cores, n => n.Equals("_Total", StringComparison.OrdinalIgnoreCase));

        var paths = CpuPaths.For(new CpuSampleOptions { IncludeCores = true, Inventory = inv }, inv);
        Assert.Equal(1, paths.Count(p => p.Instance == "_Total" && p.Counter == "% Processor Time"));
        Assert.Contains(paths, p => p.Instance == "0" && p.Counter == "% Processor Time");
        Assert.Contains(paths, p => p.Instance == "1" && p.Counter == "% Privileged Time");
        Assert.Single(paths, p => p.Counter == CpuPaths.QueueLength);
    }

    [Fact]
    public void PC01_004_missing_parking_is_omitted()
    {
        var inv = new ScriptedInventory
        {
            Present = true,
            Only = [CpuObjects.ProcessorInformation],
            Counters = ["% Processor Time", "% Privileged Time", "% User Time"],
            Instances = ["_Total", "0,0"]
        };
        var paths = CpuPaths.For(new CpuSampleOptions
        {
            IncludeParking = true,
            IncludeCores = true,
            Inventory = inv
        }, inv);
        Assert.DoesNotContain(paths, p => p.Counter == CpuPaths.ParkingStatus);
        Assert.Contains(paths, p => p.Counter == CpuPaths.QueueLength);
    }

    [Fact]
    public void PC01_004_parking_added_when_present()
    {
        var inv = new ScriptedInventory
        {
            Present = true,
            Only = [CpuObjects.ProcessorInformation],
            Counters = ["% Processor Time", "Parking Status"],
            Instances = ["_Total", "0,0"]
        };
        var paths = CpuPaths.For(new CpuSampleOptions
        {
            IncludeParking = true,
            IncludeCores = true,
            Inventory = inv
        }, inv);
        Assert.Contains(paths, p => p.Counter == CpuPaths.ParkingStatus && p.Instance == "_Total" && p.Unit == "flag");
        Assert.Contains(paths, p => p.Counter == CpuPaths.ParkingStatus && p.Instance == "0,0");
        Assert.All(paths.Where(p => p.Counter == CpuPaths.ParkingStatus), p =>
            Assert.Equal(CpuObjects.ProcessorInformation, p.Category));
    }

    [Fact]
    public void PC01_004_parking_off_skips_even_when_present()
    {
        var inv = new ScriptedInventory
        {
            Present = true,
            Only = [CpuObjects.ProcessorInformation],
            Counters = ["Parking Status"],
            Instances = ["_Total"]
        };
        var paths = CpuPaths.For(new CpuSampleOptions { IncludeParking = false, Inventory = inv }, inv);
        Assert.DoesNotContain(paths, p => p.Counter == CpuPaths.ParkingStatus);
    }

    [Fact]
    public async Task PC01_005_run_uses_shared_job()
    {
        var inv = new ScriptedInventory
        {
            Present = true,
            Only = [CpuObjects.Processor]
        };
        var paths = CpuPaths.DefaultJob("_Total", inv);
        var fake = new FakeCounterSource();
        foreach (var path in paths)
            fake.Seed(path, SampleRecord.Ok(path, 12));

        var result = await CpuPerf.RunAsync(new CpuSampleOptions
        {
            Count = 1,
            Source = fake,
            Inventory = inv,
            Clock = new ImmediateClock()
        });

        Assert.Equal(SampleStatus.Ok, result.Status);
        Assert.Equal(paths.Count, result.Samples.Count);
        Assert.All(result.Samples, s => Assert.Equal(12, s.Value));
        Assert.True(result.Samples is SampleRecord[] || ((IList<SampleRecord>)result.Samples).IsReadOnly);
    }

    [Fact]
    public void PC01_005_pid_is_not_an_api()
    {
        var names = typeof(CpuSampleOptions).GetProperties().Select(p => p.Name);
        Assert.DoesNotContain(names, n => n.Equals("Pid", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Equals("ProcessId", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Equals("ProcessName", StringComparison.OrdinalIgnoreCase));
        Assert.Null(typeof(CpuPerf).GetMethod("RunAsync", [typeof(int)]));
    }

    [Fact]
    public void PC01_005_live_total()
    {
        try
        {
            if (!PerformanceCounterCategory.Exists("Processor")
                && !PerformanceCounterCategory.Exists("Processor Information"))
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

        var inv = PdhCounterInventory.Shared;
        var obj = CpuPaths.UtilizationObject(inv);
        var path = new CounterPath(obj, "% Processor Time", "_Total", "%");
        var source = new PerformanceCounterSource();
        _ = source.Read(path);
        var row = source.Read(path);
        if (row.Status != SampleStatus.Ok)
            return;
        Assert.NotNull(row.Value);
    }

    [Fact]
    public void PC01_006_register_is_host_only()
    {
        var register = typeof(CpuPerfCatalog).GetMethod("Register");
        Assert.NotNull(register);
        Assert.Equal(typeof(VestigiumLoggerOptions), register!.GetParameters()[0].ParameterType);
        Assert.Null(typeof(CpuPerfCatalog).GetMethod("Initialize"));

        VestigiumLogger.Shutdown();
        Assert.False(VestigiumLogger.IsInitialized);
        Assert.Null(Record.Exception(() =>
            CpuPerfLog.Information(
                CpuPerfEvents.ProbeComplete,
                VestigiumStatus.Success,
                CpuPerfCatalog.Subcategories.Probe,
                "noop")));
        Assert.False(VestigiumLogger.IsInitialized);
    }

    [Fact]
    public void PC01_006_writes_noop_until_host_starts()
    {
        VestigiumLogger.Shutdown();
        var inv = new ScriptedInventory { Present = true, Only = [CpuObjects.Processor] };
        var fake = new FakeCounterSource();
        foreach (var path in CpuPaths.DefaultJob("_Total", inv))
            fake.Seed(path, SampleRecord.Ok(path, 1));

        var ex = Record.Exception(() => CpuPerf.RunAsync(new CpuSampleOptions
        {
            Count = 1,
            Source = fake,
            Inventory = inv,
            Clock = new ImmediateClock()
        }).GetAwaiter().GetResult());
        Assert.Null(ex);
        Assert.False(VestigiumLogger.IsInitialized);
    }

    [Fact]
    public void PC01_007_catalog_matches_constants()
    {
        var jsonPath = Path.Combine(
            Path.GetDirectoryName(typeof(CpuPerfCatalog).Assembly.Location)!,
            "EventCatalog",
            "cpu.json");
        Assert.True(File.Exists(jsonPath), jsonPath);

        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var root = doc.RootElement;
        Assert.Equal(CpuPerfCatalog.AppId, root.GetProperty("app").GetString());
        Assert.Equal(CpuPerfCatalog.Category, root.GetProperty("category").GetString());
        Assert.Equal(CpuPerfEvents.BlockStart, root.GetProperty("block").GetProperty("start").GetInt32());
        Assert.Equal(CpuPerfEvents.BlockEnd, root.GetProperty("block").GetProperty("end").GetInt32());

        var events = root.GetProperty("events").EnumerateArray().ToArray();
        Assert.Equal(CpuPerfCatalog.Rows.Length, events.Length);

        var constants = typeof(CpuPerfEvents)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(int) && f.Name is not ("BlockStart" or "BlockEnd"))
            .ToDictionary(f => f.Name, f => (int)f.GetValue(null)!);

        foreach (var (row, node) in CpuPerfCatalog.Rows.Zip(events))
        {
            Assert.Equal(row.EventId, node.GetProperty("eventId").GetInt32());
            Assert.Equal(row.Name, node.GetProperty("name").GetString());
            Assert.Equal(row.Severity, node.GetProperty("severity").GetString());
            Assert.Equal(row.Subcategory, node.GetProperty("subcategory").GetString());
            Assert.Equal(0, row.EventId % 5);
            Assert.InRange(row.EventId, CpuPerfEvents.BlockStart, CpuPerfEvents.BlockEnd);
            Assert.Equal(constants[row.Name], row.EventId);
        }
    }

    [Fact]
    public void PR02b_003_processor_type_agrees_with_shard()
    {
        PdhCatalogAgreement.AssertMatches(
            typeof(CpuPerfCatalog).Assembly,
            Processor.Category,
            Processor.Counters,
            CpuCounterCatalog.Counters(Processor.Category));
        Assert.Equal(Processor.PercentProcessorTime, CpuPaths.Utilization[0]);
        Assert.Equal("% Processor Time", Processor.PercentProcessorTime);
        Assert.True(CpuCounterCatalog.IsKnownCategory(CpuObjects.ProcessorInformation));
        Assert.Null(typeof(Processor).Assembly.GetType("Vestigium.Helpers.PerfMon.Cpu.ProcessorInformation"));
        Assert.Null(typeof(Processor).Assembly.GetType("Vestigium.Helpers.PerfMon.Cpu.ProcessorPerformance"));
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
