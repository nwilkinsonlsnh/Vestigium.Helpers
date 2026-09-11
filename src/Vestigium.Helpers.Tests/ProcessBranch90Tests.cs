using System.Security;
using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessBranch90Tests
{
    [Fact]
    public void Start_failures_tree_and_search_caps()
    {
        var missing = ProcessHelper.Start(new ProcessStartRequest
        {
            FileName = Path.Combine(Path.GetTempPath(), "no-such-vestigium-" + Guid.NewGuid().ToString("N") + ".exe"),
            CreateNoWindow = true
        });
        Assert.False(missing.Ok);
        Assert.Equal(ProcessStartError.FileNotFound, missing.Error);

        var badDir = ProcessHelper.Start(new ProcessStartRequest
        {
            FileName = Path.Combine(Environment.SystemDirectory, "ping.exe"),
            WorkingDirectory = Path.Combine(Path.GetTempPath(), "no-dir-" + Guid.NewGuid().ToString("N")),
            CreateNoWindow = true,
            RedirectStandardIO = true
        });
        Assert.False(badDir.Ok);

        using (var empty = new SecureString())
        {
            var noPwd = ProcessHelper.StartAs(
                new ProcessStartRequest { FileName = "cmd.exe", CreateNoWindow = true },
                new ProcessStartAs { UserName = "nobody", Password = empty, Domain = "." });
            Assert.False(noPwd.Ok);
            Assert.Equal(ProcessStartError.LogonFailed, noPwd.Error);
        }

        var noPwdNull = ProcessHelper.StartAs(
            new ProcessStartRequest { FileName = "cmd.exe", CreateNoWindow = true, LogCommandLine = true },
            new ProcessStartAs { UserName = "nobody" });
        Assert.Equal(ProcessStartError.LogonFailed, noPwdNull.Error);

        using (var pwd = Secret("x"))
        {
            var logon = ProcessHelper.StartAs(
                new ProcessStartRequest
                {
                    FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                    Arguments = "/c echo",
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.SystemDirectory,
                    LogCommandLine = true
                },
                new ProcessStartAs
                {
                    UserName = "vestigium-no-user",
                    Domain = ".",
                    Password = pwd,
                    LoadUserProfile = true,
                    LogonFlags = ProcessLogonFlags.WithProfile
                });
            Assert.False(logon.Ok);
        }

        var tree = ProcessHelper.GetTree(Environment.ProcessId, ProcessDetailLevel.Full);
        Assert.Equal(Environment.ProcessId, tree.Root.Pid);
        Assert.NotEmpty(tree.Flatten());
        Assert.Throws<InvalidOperationException>(() => ProcessHelper.GetTree(int.MaxValue));
        Assert.Empty(ProcessHelper.GetChildren(int.MaxValue - 1));
        Assert.Empty(ProcessHelper.GetDescendants(int.MaxValue - 1));
        Assert.Null(ProcessHelper.Get(int.MaxValue));
        Assert.False(ProcessHelper.TryGet(int.MaxValue, out _));
        Assert.Throws<InvalidOperationException>(() => ProcessHelper.SetComment(int.MaxValue, "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessHelper.Search("x", ProcessSearchMode.Contains, maxResults: 0));
        Assert.Throws<ArgumentException>(() => ProcessHelper.Search("x", ProcessSearchMode.Contains, maxResults: 5000));
        Assert.Throws<ArgumentException>(() => ProcessHelper.Search("PID == 1", maxResults: 5000));
        Assert.Throws<InvalidOperationException>(() => ProcessHelper.KillSearch(
            new ProcessSearchRequest { Term = "zzzz" }, new KillConfirm { Confirm = false }));
        Assert.Throws<ArgumentException>(() => ProcessHelper.KillSearch(
            new ProcessSearchRequest { Term = "zzzz" }, new KillConfirm { Confirm = true, MaxResults = 99 }));
        Assert.Empty(ProcessHelper.KillSearch(
            new ProcessSearchRequest { Term = "zzzz-no-process-vestigium", Mode = ProcessSearchMode.Contains },
            new KillConfirm { Confirm = true, MaxResults = 1 }));
        _ = ProcessHelper.GetSystemCounters();
        _ = ProcessHelper.List(ProcessDetailLevel.Identity);
        _ = ProcessHelper.GetThreads(Environment.ProcessId, includeStack: false);
    }

    [Fact]
    public async Task Watchers_campaign_run_and_comments()
    {
        using (var pidWatch = ProcessHelper.Watch(Environment.ProcessId, TimeSpan.FromMilliseconds(250), (ProcessWatchFields)0))
        {
            var sampled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            pidWatch.Sampled += (_, _) => sampled.TrySetResult(true);
            await sampled.Task.WaitAsync(TimeSpan.FromSeconds(8));
        }

        var ping = ProcessHelper.Start(new ProcessStartRequest
        {
            FileName = Path.Combine(Environment.SystemDirectory, "ping.exe"),
            Arguments = "-n 30 127.0.0.1",
            CreateNoWindow = true,
            RedirectStandardIO = true
        });
        Assert.True(ping.Ok, ping.Message);
        try
        {
            using var dying = ProcessHelper.Watch(ping.Pid!.Value, TimeSpan.FromMilliseconds(250), ProcessWatchFields.Cpu);
            var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            dying.Exited += (_, _) => exited.TrySetResult(true);
            ProcessHelper.Kill(ping.Pid.Value, force: true);
            await exited.Task.WaitAsync(TimeSpan.FromSeconds(8));
        }
        finally
        {
            try { ProcessHelper.Kill(ping.Pid!.Value, force: true); } catch { }
        }

        using (var query = ProcessHelper.Watch("PID == " + Environment.ProcessId, TimeSpan.FromMilliseconds(250), ProcessWatchFields.All))
        {
            var first = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            query.Sampled += (_, _) => first.TrySetResult(true);
            await first.Task.WaitAsync(TimeSpan.FromSeconds(8));
            var faulted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            query.Sampled += (_, sample) =>
            {
                if (sample.Matches.Count == 0)
                    faulted.TrySetResult(true);
            };
            ProcessTestHooks.QueryWatchFault = new InvalidOperationException("tick-fault");
            await faulted.Task.WaitAsync(TimeSpan.FromSeconds(8));
        }

        using (var sys = ProcessHelper.WatchSystem(TimeSpan.FromMilliseconds(250)))
            await Task.Delay(400);

        var dir = Path.Combine(Path.GetTempPath(), "VestigiumCov90", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        ProcessTestHooks.CommentStorePath = Path.Combine(dir, "c.json");
        ProcessTestHooks.CampaignRoot = Path.Combine(dir, "cam");
        ProcessTestHooks.Now = null;
        try
        {
            _ = ProcessTestHooks.Clock();
            Assert.Empty(ProcessHelper.ListCampaigns());
            File.WriteAllText(ProcessTestHooks.CommentStorePath, "{\"disk-key\":\"from-disk\"}");
            Assert.Equal("from-disk", ProcessCommentStore.Get("disk-key"));
            ProcessCommentStore.Set("disk-key", null, persist: false);

            var campaign = ProcessHelper.CreateCampaign(new ProcessCampaignRecipe
            {
                Name = "run-async",
                Query = "PID == " + Environment.ProcessId,
                Fields = 0,
                IncludeSystemCounters = false,
                SampleInterval = TimeSpan.FromMilliseconds(250),
                Windows = [new ProcessCampaignWindow("all-day", new TimeOnly(0, 0), TimeSpan.FromHours(24), ProcessCampaignDays.All)]
            });
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            await campaign.RunAsync(cts.Token);
            Assert.Equal(ProcessCampaignState.Stopped, campaign.State);
            campaign.Dispose();
            Assert.Throws<FileNotFoundException>(() => ProcessHelper.LoadCampaign("missing-campaign"));
        }
        finally
        {
            ProcessTestHooks.CommentStorePath = null;
            ProcessTestHooks.CampaignRoot = null;
            ProcessTestHooks.Now = null;
            ProcessTestHooks.QueryWatchFault = null;
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void Kql_level_and_autostart_hash()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        Assert.True(ProcessKqlLevel.NeedsFull(KqlHelper.Parse("PID == 1 AND CommandLine LIKE '%x%'").Expression!, session));
        Assert.False(ProcessKqlLevel.NeedsFull(KqlHelper.Parse("PID == 1 OR Name LIKE 'a%'").Expression!, session));
        Assert.NotNull(ProcessAutostart.Locate(@"C:\Windows\System32\notepad.exe", "notepad.exe"));
        Assert.Equal(ProcessCommentStore.Hash("A"), ProcessCommentStore.Hash("a"));
        Assert.True(ProcessHelper.MatchSystem("CPU.LogicalProcessors GT 0 OR SYS.ProcessCount GT 0"));
        _ = ProcessHelper.DefaultCampaignRoot;
    }

    private static SecureString Secret(string text)
    {
        var pwd = new SecureString();
        foreach (var ch in text)
            pwd.AppendChar(ch);
        pwd.MakeReadOnly();
        return pwd;
    }
}
