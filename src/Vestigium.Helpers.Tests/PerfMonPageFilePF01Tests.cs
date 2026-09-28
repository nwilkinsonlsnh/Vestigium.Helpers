using System.Diagnostics;
using System.Text.Json;
using Vestigium.Helpers.PerfMon;
using Vestigium.Helpers.PerfMon.PageFile;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
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

    [Fact]
    public async Task PF01_005_run_uses_shared_job()
    {
        var paths = PageFilePaths.Usage("_Total");
        var fake = new FakeCounterSource();
        foreach (var path in paths)
            fake.Seed(path, SampleRecord.Ok(path, 12));

        var result = await PageFilePerf.RunAsync(new PageFileSampleOptions
        {
            Count = 1,
            Source = fake,
            Clock = new ImmediateClock()
        });

        Assert.Equal(SampleStatus.Ok, result.Status);
        Assert.Equal(paths.Count, result.Samples.Count);
        Assert.All(result.Samples, s =>
        {
            Assert.Equal(PageFileObjects.PagingFile, s.Category);
            Assert.Equal(12, s.Value);
        });
    }

    [Fact]
    public void PF01_005_live_total()
    {
        try
        {
            if (!PerformanceCounterCategory.Exists(PageFileObjects.PagingFile))
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

        var path = new CounterPath(PageFileObjects.PagingFile, "% Usage", "_Total", "%");
        var source = new PerformanceCounterSource();
        var row = source.Read(path);
        if (row.Status != SampleStatus.Ok)
            return;
        Assert.NotNull(row.Value);
    }

    [Fact]
    public void PF01_006_register_is_host_only()
    {
        var register = typeof(PageFilePerfCatalog).GetMethod("Register");
        Assert.NotNull(register);
        Assert.Equal(typeof(VestigiumLoggerOptions), register!.GetParameters()[0].ParameterType);
        Assert.Null(typeof(PageFilePerfCatalog).GetMethod("Initialize"));

        VestigiumLogger.Shutdown();
        Assert.False(VestigiumLogger.IsInitialized);
        Assert.Null(Record.Exception(() =>
            PageFilePerfLog.Information(
                PageFilePerfEvents.ProbeComplete,
                VestigiumStatus.Success,
                PageFilePerfCatalog.Subcategories.Probe,
                "noop")));
        Assert.False(VestigiumLogger.IsInitialized);
    }

    [Fact]
    public void PF01_006_writes_noop_until_host_starts()
    {
        VestigiumLogger.Shutdown();
        var fake = new FakeCounterSource();
        foreach (var path in PageFilePaths.Usage("_Total"))
            fake.Seed(path, SampleRecord.Ok(path, 1));

        var ex = Record.Exception(() => PageFilePerf.RunAsync(new PageFileSampleOptions
        {
            Count = 1,
            Source = fake,
            Clock = new ImmediateClock()
        }).GetAwaiter().GetResult());
        Assert.Null(ex);
        Assert.False(VestigiumLogger.IsInitialized);
    }

    [Fact]
    public void PF01_007_catalog_matches_constants()
    {
        var jsonPath = Path.Combine(
            Path.GetDirectoryName(typeof(PageFilePerfCatalog).Assembly.Location)!,
            "EventCatalog",
            "pagefile.json");
        Assert.True(File.Exists(jsonPath), jsonPath);

        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var root = doc.RootElement;
        Assert.Equal(PageFilePerfCatalog.AppId, root.GetProperty("app").GetString());
        Assert.Equal(PageFilePerfCatalog.Category, root.GetProperty("category").GetString());
        Assert.Equal(PageFilePerfEvents.BlockStart, root.GetProperty("block").GetProperty("start").GetInt32());
        Assert.Equal(PageFilePerfEvents.BlockEnd, root.GetProperty("block").GetProperty("end").GetInt32());

        var events = root.GetProperty("events").EnumerateArray().ToArray();
        Assert.Equal(PageFilePerfCatalog.Rows.Length, events.Length);

        var constants = typeof(PageFilePerfEvents)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(int) && f.Name is not ("BlockStart" or "BlockEnd"))
            .ToDictionary(f => f.Name, f => (int)f.GetValue(null)!);

        foreach (var (row, node) in PageFilePerfCatalog.Rows.Zip(events))
        {
            Assert.Equal(row.EventId, node.GetProperty("eventId").GetInt32());
            Assert.Equal(row.Name, node.GetProperty("name").GetString());
            Assert.Equal(row.Severity, node.GetProperty("severity").GetString());
            Assert.Equal(row.Subcategory, node.GetProperty("subcategory").GetString());
            Assert.Equal(0, row.EventId % 5);
            Assert.InRange(row.EventId, PageFilePerfEvents.BlockStart, PageFilePerfEvents.BlockEnd);
            Assert.Equal(constants[row.Name], row.EventId);
        }
    }

    [Fact]
    public void PF01_008_probe_references_shared_only()
    {
        var text = File.ReadAllText(FindCsproj());
        Assert.Contains("Vestigium.Helpers.PerfMon\\Vestigium.Helpers.PerfMon.csproj", text);
        Assert.DoesNotContain("Vestigium.Helpers.PerfMon.Memory", text);
        Assert.DoesNotContain("Vestigium.Helpers.Charts", text);
        Assert.DoesNotContain("Vestigium.Helpers.Analytics", text);
        Assert.DoesNotContain("Vestigium.Helpers.FileIo", text);
        Assert.DoesNotContain("Vestigium.Helpers.Processes", text);
        Assert.DoesNotContain("System.Diagnostics.PerformanceCounter", text);
        Assert.Null(typeof(PageFileSampleOptions).GetProperty("IncludeMemoryRates"));
        Assert.Null(typeof(PageFilePerf).GetMethod("Initialize"));
    }

    private static string FindCsproj()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var hit = dir.GetFiles("Vestigium.Helpers.PerfMon.PageFile.csproj", SearchOption.AllDirectories).FirstOrDefault();
            if (hit is not null)
                return hit.FullName;
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Vestigium.Helpers.PerfMon.PageFile.csproj");
    }

    [Fact]
    public void PR02b_001_paths_use_typed_pagingfile()
    {
        Assert.Equal(PagingFile.Category, PageFilePaths.ObjectName);
        Assert.Equal(PagingFile.PercentUsage, PageFilePaths.ShortCounters[0]);
        Assert.Equal(PagingFile.PercentUsagePeak, PageFilePaths.ShortCounters[1]);
        var paths = PageFilePaths.Usage();
        Assert.All(paths, row => Assert.Equal(PagingFile.Category, row.Category));
        Assert.Contains(paths, row => row.Counter == PagingFile.PercentUsage);
        Assert.Contains(paths, row => row.Counter == PagingFile.PercentUsagePeak);
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
