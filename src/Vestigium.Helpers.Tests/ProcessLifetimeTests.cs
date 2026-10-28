using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessLifetimeTests
{
    [Fact]
    public void Start_missing_file_is_file_not_found()
    {
        var result = ProcessHelper.Start(new ProcessStartRequest
        {
            FileName = Path.Combine(Path.GetTempPath(), "vestigium-missing-" + Guid.NewGuid().ToString("N") + ".exe")
        });
        Assert.False(result.Ok);
        Assert.Equal(ProcessStartError.FileNotFound, result.Error);
    }

    [Fact]
    public void Start_then_kill_disposable_child()
    {
        var started = ProcessHelper.Start(new ProcessStartRequest
        {
            FileName = Path.Combine(Environment.SystemDirectory, "ping.exe"),
            Arguments = "-n 30 127.0.0.1",
            CreateNoWindow = true
        });
        Assert.True(started.Ok, started.Message);
        Assert.True(started.Pid > 0);
        var pid = started.Pid!.Value;
        try
        {
            Assert.NotNull(ProcessHelper.Get(pid, ProcessDetailLevel.Identity));
            var killed = ProcessHelper.Kill(pid, force: true);
            Assert.Equal(ProcessKillStatus.Ok, killed.Status);
        }
        finally
        {
            try { ProcessHelper.Kill(pid, force: true); } catch { }
        }
    }

    [Fact]
    public void Kill_unknown_pid_is_gone()
        => Assert.Equal(ProcessKillStatus.Gone, ProcessHelper.Kill(int.MaxValue - 13).Status);

    [Fact]
    public void Kill_lsass_is_denied()
    {
        var hits = ProcessHelper.Search("lsass.exe", ProcessSearchMode.Contains, ProcessSearchFields.Name, maxResults: 4);
        if (hits.Count == 0)
            return;
        var result = ProcessHelper.Kill(hits[0].Pid, force: true);
        Assert.Equal(ProcessKillStatus.Denied, result.Status);
    }

    [Fact]
    public void KillSearch_requires_confirm()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ProcessHelper.KillSearch(
                new ProcessSearchRequest { Term = "ping", Mode = ProcessSearchMode.Contains },
                new KillConfirm { Confirm = false }));
    }

    [Fact]
    public void StartAs_rejects_blank_user()
        => Assert.Throws<ArgumentException>(() =>
            ProcessHelper.StartAs(
                new ProcessStartRequest { FileName = "cmd.exe" },
                new ProcessStartAs { UserName = "  " }));
}
