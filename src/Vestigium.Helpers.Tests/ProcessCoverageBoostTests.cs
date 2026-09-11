using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Processes;
using ProcessThreadState = Vestigium.Helpers.Processes.ThreadState;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessCoverageBoostTests
{
    [Fact]
    public void ProcessKqlRow_populated_unknown_denied_and_extras()
    {
        var filled = new ProcessInfo
        {
            Pid = 42,
            ParentPid = 1,
            Name = "app.exe",
            SessionId = 1,
            ImagePath = @"C:\Windows\System32\app.exe",
            ImageType = ProcessImageType.X64,
            Description = "desc",
            CompanyName = "co",
            Version = "1.0",
            VerifiedSigner = new SignerInfo(SignerTrust.Verified, "Pub", "Iss", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            PackageName = "pkg",
            CommandLine = "app.exe -x",
            Comment = "c",
            AutostartLocation = "HKCU Run",
            WindowTitle = "Title",
            WindowStatus = WindowStatus.Visible,
            CpuTime = TimeSpan.FromSeconds(3),
            CpuPercent = 4.5,
            PrivateBytes = 10,
            WorkingSet = 11,
            IoReads = 1,
            IoReadBytes = 2,
            IoWrites = 3,
            IoWriteBytes = 4,
            GpuUsagePercent = 5,
            GpuDedicatedBytes = 6,
            GpuSystemBytes = 7,
            IntegrityLevel = IntegrityLevel.Medium,
            DepStatus = DepStatus.Enabled,
            AslrEnabled = true,
            ControlFlowGuard = MitigationState.Enabled,
            StackProtection = MitigationState.Enabled,
            UiAccess = false,
            Virtualized = false,
            Protection = new ProcessProtection("None", null),
            DpiAwareness = DpiAwareness.PerMonitorV2,
            EnterpriseContext = "none"
        };
        var extras = new Dictionary<string, object?> { ["CPU.Usage"] = 12.5, ["IO.ReadDelta"] = 3L };
        var row = new ProcessKqlRow(filled, extras);
        Assert.False(row.Get("CPU.Usage").IsUnknown);
        Assert.False(row.Get("IO.ReadDelta").IsUnknown);
        foreach (var name in new[]
                 {
                     "PROC.Pid", "PROC.ParentPid", "PROC.Name", "PROC.ImagePath", "PROC.CommandLine", "PROC.Session",
                     "PROC.WindowTitle", "PROC.WindowStatus", "PROC.Company", "PROC.Description", "PROC.Version",
                     "PROC.Signer", "PROC.SignerTrust", "PROC.Package", "PROC.Autostart", "PROC.Comment",
                     "PROC.ImageType", "PROC.Integrity", "PROC.Dep", "PROC.Aslr", "PROC.Cfg", "PROC.StackProtection",
                     "PROC.Protection", "PROC.Dpi", "PROC.UiAccess", "PROC.Virtualized", "PROC.EnterpriseContext",
                     "CPU.Time", "MEM.PrivateBytes", "MEM.WorkingSet", "IO.Reads", "IO.ReadBytes", "IO.Writes",
                     "IO.WriteBytes", "GPU.Usage", "GPU.DedicatedMemory", "GPU.SystemMemory"
                 })
            Assert.False(row.Get(name).IsUnknown, name);
        Assert.True(row.Get("nope").IsUnknown);

        var empty = new ProcessKqlRow(new ProcessInfo { Pid = 1, Name = "x" });
        Assert.True(empty.Get("PROC.CommandLine").IsUnknown);
        Assert.True(empty.Get("PROC.WindowStatus").IsUnknown);
        Assert.True(empty.Get("PROC.ImageType").IsUnknown);
        Assert.True(empty.Get("PROC.Cfg").IsUnknown);
        Assert.True(empty.Get("PROC.Dep").IsUnknown);
        Assert.True(empty.Get("PROC.Protection").IsUnknown);

        var denied = new ProcessKqlRow(new ProcessInfo
        {
            Pid = 2,
            Name = "y",
            CommandLine = "secret",
            CpuPercent = 9,
            Availability =
            [
                new FieldAvailability(ProcessField.CommandLine, Availability.Denied, "acl"),
                new FieldAvailability(ProcessField.CpuPercent, Availability.Unsupported, "watcher"),
                new FieldAvailability(ProcessField.PrivateBytes, Availability.Gone, "dead")
            ],
            PrivateBytes = 8
        });
        Assert.True(denied.Get("PROC.CommandLine").IsUnknown);
        Assert.True(denied.Get("CPU.Usage").IsUnknown);
        Assert.True(denied.Get("MEM.PrivateBytes").IsUnknown);
    }

    [Fact]
    public void System_and_thread_kql_rows_cover_catalog()
    {
        var counters = new SystemCounters
        {
            CpuPercent = 1,
            SystemCommitPercent = 2,
            PhysicalMemoryPercent = 3,
            IoThroughputBytesPerSec = 4,
            GpuUsagePercent = 5,
            GpuDedicatedBytes = 6,
            GpuSystemBytes = 7,
            ReadDelta = 8,
            WriteDelta = 9,
            OtherDelta = 10,
            ReadBytesDelta = 11,
            WriteBytesDelta = 12,
            OtherBytesDelta = 13,
            CommitCurrent = 14,
            CommitLimit = 15,
            CommitPeak = 16,
            CommitChange = 17,
            PhysicalTotal = 18,
            PhysicalAvailable = 19,
            CacheWorkingSet = 20,
            KernelWorkingSet = 21,
            DriverWorkingSet = 22,
            PagedWorkingSet = 23,
            Nonpaged = 24,
            PagedLimit = 25,
            NonpagedLimit = 26,
            Zeroed = 27,
            Free = 28,
            Modified = 29,
            Standby = 30,
            PageFaultDelta = 31,
            PageReadDelta = 32,
            PagingFileWriteDelta = 33,
            MappedFileWriteDelta = 34,
            ProcessCount = 35,
            ThreadCount = 36,
            HandleCount = 37,
            ContextSwitchDelta = 38,
            InterruptDelta = 39,
            DpcDelta = 40,
            Cores = 41,
            Sockets = 42,
            LogicalProcessors = 43
        };
        var sys = new SystemKqlRow(counters);
        using var session = KqlHelper.Create(KqlPack.System);
        foreach (var field in session.Fields)
            _ = sys.Get(field.Canonical);
        Assert.False(sys.Get("SYS.ProcessCount").IsUnknown);
        Assert.True(new SystemKqlRow(new SystemCounters()).Get("SYS.ProcessCount").IsUnknown);
        Assert.True(sys.Get("nope").IsUnknown);

        var empty = new SystemCounters();
        Assert.Null(empty.CommitCurrentK);
        Assert.Null(empty.CommitLimitK);
        Assert.Null(empty.CommitPeakK);
        Assert.Null(empty.PhysicalTotalK);
        Assert.Null(empty.PhysicalAvailableK);
        Assert.Null(empty.CacheWorkingSetK);
        Assert.Equal(14 / 1024, counters.CommitCurrentK);
        Assert.Equal(15 / 1024, counters.CommitLimitK);
        Assert.Equal(16 / 1024, counters.CommitPeakK);
        Assert.Equal(18 / 1024, counters.PhysicalTotalK);
        Assert.Equal(19 / 1024, counters.PhysicalAvailableK);
        Assert.Equal(20 / 1024, counters.CacheWorkingSetK);

        var thread = new ThreadKqlRow(new ThreadInfo
        {
            ThreadId = 9,
            ProcessId = 1,
            State = ProcessThreadState.Running,
            StartAddress = "0x1",
            CpuPercent = 3.2
        });
        Assert.False(thread.Get("THR.Tid").IsUnknown);
        Assert.False(thread.Get("THR.State").IsUnknown);
        Assert.False(thread.Get("THR.StartAddress").IsUnknown);
        Assert.False(thread.Get("THR.Cpu").IsUnknown);
        Assert.False(thread.Get("CPU.Usage").IsUnknown);
        var blank = new ThreadKqlRow(new ThreadInfo { ThreadId = 1, ProcessId = 1, State = ProcessThreadState.Unknown });
        Assert.True(blank.Get("THR.State").IsUnknown);
        Assert.True(blank.Get("THR.Cpu").IsUnknown);
        Assert.True(blank.Get("nope").IsUnknown);
    }

    [Fact]
    public void Image_path_campaign_delta_and_sort_edges()
    {
        Assert.Equal("n", ProcessImagePath.Normalize(null, "n"));
        Assert.Equal("n", ProcessImagePath.Normalize("  ", "n"));
        Assert.Contains("System32", ProcessImagePath.Normalize(@"C:\Windows\Sysnative\a.exe", "a.exe"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("System32", ProcessImagePath.Normalize(@"C:/Windows/SysWOW64/a.exe", "a.exe"), StringComparison.OrdinalIgnoreCase);

        var zone = TimeZoneInfo.Local;
        var wrap = new ProcessCampaignWindow("night", new TimeOnly(23, 0), TimeSpan.FromHours(3), ProcessCampaignDays.All);
        Assert.True(ProcessCampaign.IsOpen(wrap, Wall(zone, 2026, 9, 12, 0, 30), zone));
        Assert.False(ProcessCampaign.IsOpen(wrap, Wall(zone, 2026, 9, 11, 22, 0), zone));
        var sat = Wall(zone, 2026, 9, 12, 10, 0);
        Assert.False(ProcessCampaign.IsOpen(new ProcessCampaignWindow("w", new TimeOnly(9, 0), TimeSpan.FromHours(2), ProcessCampaignDays.Weekdays), sat, zone));
        Assert.True(ProcessCampaign.IsOpen(new ProcessCampaignWindow("w", new TimeOnly(9, 0), TimeSpan.FromHours(2), ProcessCampaignDays.Weekend), sat, zone));
        Assert.True(ProcessCampaign.IsOpen(new ProcessCampaignWindow("w", new TimeOnly(9, 0), TimeSpan.FromHours(2), 0), sat, zone));

        foreach (var day in new[]
                 {
                     (Wall(zone, 2026, 9, 13, 10, 0), ProcessCampaignDays.Sunday),
                     (Wall(zone, 2026, 9, 14, 10, 0), ProcessCampaignDays.Monday),
                     (Wall(zone, 2026, 9, 15, 10, 0), ProcessCampaignDays.Tuesday),
                     (Wall(zone, 2026, 9, 16, 10, 0), ProcessCampaignDays.Wednesday),
                     (Wall(zone, 2026, 9, 17, 10, 0), ProcessCampaignDays.Thursday),
                     (Wall(zone, 2026, 9, 18, 10, 0), ProcessCampaignDays.Friday),
                     (Wall(zone, 2026, 9, 19, 10, 0), ProcessCampaignDays.Saturday)
                 })
        {
            var window = new ProcessCampaignWindow("d", new TimeOnly(9, 0), TimeSpan.FromHours(2), day.Item2);
            Assert.True(ProcessCampaign.IsOpen(window, day.Item1, zone));
        }

        Assert.Equal("a-b", ProcessCampaign.Sanitize("a b!"));
        Assert.Equal("campaign", ProcessCampaign.Sanitize("***"));

        Assert.Throws<ArgumentException>(() => ProcessCampaign.Validate(new ProcessCampaignRecipe
        {
            Name = "x",
            Query = "Nope == 1",
            Windows = [new ProcessCampaignWindow("m", new TimeOnly(8, 0), TimeSpan.FromMinutes(10), ProcessCampaignDays.All)]
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessCampaign.Validate(new ProcessCampaignRecipe
        {
            Name = "x",
            Query = "PID == 1",
            Windows = [new ProcessCampaignWindow("m", new TimeOnly(8, 0), TimeSpan.FromSeconds(30), ProcessCampaignDays.All)]
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessCampaign.Validate(new ProcessCampaignRecipe
        {
            Name = "x",
            Query = "PID == 1",
            Windows = [new ProcessCampaignWindow("m", new TimeOnly(8, 0), TimeSpan.FromHours(25), ProcessCampaignDays.All)]
        }));
        Assert.Throws<ArgumentException>(() => ProcessCampaign.Validate(new ProcessCampaignRecipe
        {
            Name = "x",
            Windows = [new ProcessCampaignWindow("m", new TimeOnly(8, 0), TimeSpan.FromMinutes(10), ProcessCampaignDays.All)]
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessCampaign.Validate(new ProcessCampaignRecipe
        {
            Name = "x",
            Query = "PID == 1",
            MaxMatches = 300,
            Windows = [new ProcessCampaignWindow("m", new TimeOnly(8, 0), TimeSpan.FromMinutes(10), ProcessCampaignDays.All)]
        }));

        var map = new ProcessDeltaMap();
        var a = new ProcessInfo { Pid = 9, Name = "a", CpuTime = TimeSpan.FromSeconds(1), PrivateBytes = 1, WorkingSet = 1, IoReads = 1, IoReadBytes = 1, IoWrites = 1, IoWriteBytes = 1 };
        var b = new ProcessInfo { Pid = 9, Name = "a", CpuTime = TimeSpan.FromSeconds(2), PrivateBytes = 3, WorkingSet = 4, IoReads = 5, IoReadBytes = 6, IoWrites = 7, IoWriteBytes = 8 };
        Assert.Empty(map.Remember(a, TimeSpan.FromSeconds(1)));
        var extras = map.Remember(b, TimeSpan.FromSeconds(1));
        Assert.True(extras.ContainsKey("CPU.Usage"));
        Assert.Equal(2L, extras["MEM.PrivateBytesDelta"]);
        Assert.False(ProcessDeltaMap.Diff(b, a, TimeSpan.Zero).ContainsKey("CPU.Usage"));
        map.Prune(new HashSet<int> { 1 });
        map.Clear();

        var sorted = ProcessSearchSort.TakeStable(
        [
            new ProcessInfo { Pid = 2, Name = "b" },
            new ProcessInfo { Pid = 1, Name = "a" },
            new ProcessInfo { Pid = 3, Name = "a" }
        ], 2);
        Assert.Equal([1, 3], sorted.Select(r => r.Pid));
        Assert.Empty(ProcessSearchSort.TakeStable([], 5));
    }

    [Fact]
    public void Helper_public_edges_and_live_snapshots()
    {
        Assert.Equal(ProcessHelper.Identity, ProcessHelper.Probe());
        Assert.False(ProcessHelper.TryGet(0, out _));
        Assert.NotEmpty(ProcessHelper.List(ProcessDetailLevel.Full));
        Assert.NotEmpty(ProcessHelper.GetThreads(Environment.ProcessId, includeStack: true));
        Assert.NotEmpty(ProcessHelper.Search("PID == " + Environment.ProcessId));
        Assert.Empty(ProcessHelper.Search("zzzz-no-hit", (ProcessSearchMode)99, ProcessSearchFields.Name, ProcessDetailLevel.Identity, 4));
        var self = ProcessHelper.Get(Environment.ProcessId, ProcessDetailLevel.Full);
        Assert.NotNull(self);
        Assert.NotEmpty(ProcessHelper.Search(self.Name, ProcessSearchMode.Contains, ProcessSearchFields.Name, ProcessDetailLevel.Identity, 32));
        if (!string.IsNullOrWhiteSpace(self.ImagePath))
            Assert.NotNull(ProcessHelper.Search(Path.GetFileName(self.ImagePath), ProcessSearchMode.Contains, ProcessSearchFields.ImagePath, ProcessDetailLevel.Identity, 32));
        _ = ProcessHelper.Search("no-window-title-zzzz", ProcessSearchMode.Contains, ProcessSearchFields.WindowTitle, ProcessDetailLevel.Slim, 4);
        _ = ProcessHelper.Search("no-cmd-zzzz", ProcessSearchMode.Contains, ProcessSearchFields.CommandLine, ProcessDetailLevel.Slim, 4);
        Assert.NotNull(ProcessAutostart.Locate(self.ImagePath, self.Name));
        Assert.NotNull(ProcessAutostart.Locate(null, self.Name));
        Assert.True(ProcessHelper.MatchSystem("SYS.ProcessCount GT 0"));
        Assert.False(ProcessHelper.MatchSystem("SYS.ProcessCount == 0"));
        Assert.Throws<ArgumentException>(() => ProcessHelper.MatchSystem("Nope == 1"));
        Assert.Throws<ArgumentException>(() => ProcessHelper.Search("Nope == 1"));
        Assert.Throws<ArgumentException>(() => ProcessHelper.SearchThreads(Environment.ProcessId, "Nope == 1"));
        Assert.Throws<ArgumentException>(() => ProcessHelper.Watch("Nope == 1", TimeSpan.FromSeconds(1), ProcessWatchFields.All));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessHelper.WatchSystem(TimeSpan.FromMilliseconds(249)));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessHelper.GetChildren(0));
        var threads = ProcessHelper.SearchThreads(Environment.ProcessId, "TID GT 0");
        Assert.NotEmpty(threads);
        ProcessHelper.GetChildren(Environment.ProcessId);
        ProcessHelper.GetDescendants(Environment.ProcessId);
        ProcessHelper.SetComment(Environment.ProcessId, "cov", persist: false);

        var started = ProcessHelper.Start(new ProcessStartRequest
        {
            FileName = Path.Combine(Environment.SystemDirectory, "ping.exe"),
            Arguments = "-n 20 127.0.0.1",
            WorkingDirectory = Environment.SystemDirectory,
            CreateNoWindow = true,
            RedirectStandardIO = true,
            Environment = new Dictionary<string, string> { ["VESTIGIUM_COV"] = "1" }
        });
        Assert.True(started.Ok, started.Message);
        try
        {
            var soft = ProcessHelper.Kill(started.Pid!.Value, force: false);
            Assert.True(soft.Status is ProcessKillStatus.Failed or ProcessKillStatus.Ok or ProcessKillStatus.Gone);
            var tree = ProcessHelper.KillTree(started.Pid.Value, force: true);
            Assert.Contains(tree, item => item.Pid == started.Pid);
        }
        finally
        {
            try { ProcessHelper.Kill(started.Pid!.Value, force: true); } catch { }
        }

        Assert.True(ProcessKiller.IsGuarded(new ProcessInfo { Pid = 1, Name = "lsass.exe" }));
        Assert.True(ProcessKiller.IsGuarded(new ProcessInfo { Pid = 1, Name = "x", IntegrityLevel = IntegrityLevel.Protected }));
        Assert.True(ProcessKiller.IsGuarded(new ProcessInfo { Pid = 1, Name = "x", Protection = new ProcessProtection("PPL", "WinTcb") }));
        Assert.False(ProcessKiller.IsGuarded(new ProcessInfo { Pid = 1, Name = "x", Protection = new ProcessProtection("None", null) }));
        Assert.Equal(ProcessKillStatus.Denied, ProcessHelper.KillTree(1, force: true)[^1].Status);
    }

    [Fact]
    public void Comment_store_and_campaign_truncate()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumCov", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        ProcessTestHooks.CommentStorePath = Path.Combine(dir, "c.json");
        ProcessTestHooks.CampaignRoot = Path.Combine(dir, "cam");
        var now = new DateTimeOffset(2026, 9, 10, 8, 1, 0, TimeSpan.FromHours(-4));
        ProcessTestHooks.Now = () => now;
        try
        {
            var key = ProcessCommentStore.Key(@"C:\Windows\System32\x.exe", "x.exe");
            ProcessCommentStore.Set(key, "hi", persist: true);
            Assert.Equal("hi", ProcessCommentStore.Get(key));
            ProcessCommentStore.Set(key, "  ", persist: true);
            File.WriteAllText(ProcessTestHooks.CommentStorePath, "{not-json");
            _ = ProcessCommentStore.Get("missing");

            var campaign = ProcessHelper.CreateCampaign(new ProcessCampaignRecipe
            {
                Name = "cov both",
                Match = new ProcessSearchRequest { Term = "unused", Mode = ProcessSearchMode.Contains },
                Query = "PID GT 0",
                MaxMatches = 1,
                SampleInterval = TimeSpan.FromMilliseconds(250),
                IncludeSystemCounters = true,
                TimeZoneId = "Not/AZone",
                Windows = [new ProcessCampaignWindow("morning", new TimeOnly(8, 0), TimeSpan.FromMinutes(10), ProcessCampaignDays.All)]
            });
            var opened = false;
            campaign.WindowChanged += (_, e) => opened |= e.Open;
            campaign.TickOnce();
            Assert.True(opened);
            Assert.Equal(ProcessCampaignState.Sampling, campaign.State);
            Assert.True(File.Exists(campaign.SamplePath));
            Assert.Contains("cov-both", ProcessHelper.ListCampaigns());
            var loaded = ProcessHelper.LoadCampaign("cov-both");
            Assert.Equal(campaign.CampaignId, loaded.CampaignId);

            ProcessTestHooks.Now = () => new DateTimeOffset(2026, 9, 10, 7, 0, 0, TimeSpan.FromHours(-4));
            campaign.TickOnce();
            Assert.Equal(ProcessCampaignState.Waiting, campaign.State);
            campaign.Stop();
            campaign.Dispose();
            campaign.Dispose();

            var termOnly = ProcessHelper.CreateCampaign(new ProcessCampaignRecipe
            {
                Name = "term-only",
                Match = new ProcessSearchRequest { Term = "testhost", Mode = ProcessSearchMode.Contains },
                IncludeSystemCounters = false,
                SampleInterval = TimeSpan.FromMilliseconds(250),
                Windows = [new ProcessCampaignWindow("morning", new TimeOnly(8, 0), TimeSpan.FromMinutes(10), ProcessCampaignDays.All)]
            });
            ProcessTestHooks.Now = () => now;
            termOnly.TickOnce();
            termOnly.Dispose();
        }
        finally
        {
            ProcessTestHooks.CommentStorePath = null;
            ProcessTestHooks.CampaignRoot = null;
            ProcessTestHooks.Now = null;
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void Kql_level_walks_in_between_and_not()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        Assert.True(ProcessKqlLevel.NeedsFull(KqlHelper.Parse("CommandLine LIKE '%x%'").Expression!, session));
        Assert.True(ProcessKqlLevel.NeedsFull(KqlHelper.Parse("Description IN ('a')").Expression!, session));
        Assert.True(ProcessKqlLevel.NeedsFull(KqlHelper.Parse("NOT (WindowTitle == 'x')").Expression!, session));
        Assert.False(ProcessKqlLevel.NeedsFull(KqlHelper.Parse("PID BETWEEN 1 AND 2").Expression!, session));
        Assert.Equal(ProcessDetailLevel.Full, ProcessKqlLevel.Resolve(ProcessDetailLevel.Full, KqlHelper.Parse("PID == 1").Expression!, session));
        Assert.Equal(ProcessDetailLevel.Full, ProcessKqlLevel.Resolve(ProcessDetailLevel.Slim, KqlHelper.Parse("CommandLine LIKE '%x%'").Expression!, session));
        Assert.Equal(ProcessDetailLevel.Slim, ProcessKqlLevel.Resolve(ProcessDetailLevel.Identity, KqlHelper.Parse("PID == 1").Expression!, session));
    }

    private static DateTimeOffset Wall(TimeZoneInfo zone, int year, int month, int day, int hour, int minute)
    {
        var local = new DateTime(year, month, day, hour, minute, 0);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }
}
