using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessSafetyTests
{
    [Fact]
    public void Denylist_name_is_guarded()
    {
        Assert.True(ProcessKiller.IsGuarded(new ProcessInfo { Pid = 4, Name = "System" }));
        Assert.True(ProcessKiller.IsGuarded(new ProcessInfo { Pid = 500, Name = "lsass.exe" }));
        Assert.True(ProcessKiller.IsGuarded(new ProcessInfo { Pid = 8, Name = "app.exe", IntegrityLevel = IntegrityLevel.Protected }));
        Assert.False(ProcessKiller.IsGuarded(new ProcessInfo { Pid = 9, Name = "notepad.exe" }));
    }

    [Fact]
    public void Kill_system_process_is_denied()
    {
        var result = ProcessHelper.Kill(4, force: true);
        Assert.True(result.Status is ProcessKillStatus.Denied or ProcessKillStatus.Gone, result.Status + " " + result.Message);
        if (result.Status == ProcessKillStatus.Denied)
            Assert.Contains("protected", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KillSearch_confirm_does_not_override_denylist()
    {
        var results = ProcessHelper.KillSearch(
            new ProcessSearchRequest { Term = "lsass", Mode = ProcessSearchMode.Contains, Fields = ProcessSearchFields.Name },
            new KillConfirm { Confirm = true, MaxResults = 4 });
        Assert.All(results, item => Assert.Equal(ProcessKillStatus.Denied, item.Status));
    }

    [Fact]
    public void Search_cap_is_stable_across_calls()
    {
        var a = ProcessHelper.Search(".exe", ProcessSearchMode.EndsWith, ProcessSearchFields.Name, ProcessDetailLevel.Identity, 8);
        var b = ProcessHelper.Search(".exe", ProcessSearchMode.EndsWith, ProcessSearchFields.Name, ProcessDetailLevel.Identity, 8);
        Assert.Equal(a.Select(row => row.Pid), b.Select(row => row.Pid));
        Assert.Equal(a.Select(row => row.Name), a.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.Pid).Select(row => row.Name));
    }

    [Fact]
    public void Kql_search_cap_is_stable_across_calls()
    {
        var a = ProcessHelper.Search("Name LIKE '%.exe'", ProcessDetailLevel.Identity, 8);
        var b = ProcessHelper.Search("Name LIKE '%.exe'", ProcessDetailLevel.Identity, 8);
        Assert.Equal(a.Select(row => row.Pid), b.Select(row => row.Pid));
    }
}
