using System.Diagnostics;
using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessTreeTests
{
    [Fact]
    public void GetThreads_of_host_has_at_least_one_tid()
    {
        var threads = ProcessHelper.GetThreads(Environment.ProcessId);
        Assert.NotEmpty(threads);
        Assert.Contains(threads, row => row.ThreadId > 0 && row.ProcessId == Environment.ProcessId);
    }

    [Fact]
    public void GetTree_rejects_non_positive_pid()
        => Assert.Throws<ArgumentOutOfRangeException>(() => ProcessHelper.GetTree(0));

    [Fact]
    public void GetTree_gone_pid_throws()
        => Assert.Throws<InvalidOperationException>(() => ProcessHelper.GetTree(int.MaxValue - 11));

    [Fact]
    public void Child_fixture_appears_under_host()
    {
        using var child = StartPing();
        try
        {
            Assert.True(WaitForChild(child.Id), "child did not appear in the process table");
            var kids = ProcessHelper.GetChildren(Environment.ProcessId);
            Assert.Contains(kids, row => row.Pid == child.Id);
            var tree = ProcessHelper.GetTree(Environment.ProcessId);
            Assert.Equal(Environment.ProcessId, tree.Root.Pid);
            Assert.Contains(tree.Flatten(), row => row.Pid == child.Id);
            Assert.Contains(ProcessHelper.GetDescendants(Environment.ProcessId), row => row.Pid == child.Id);
        }
        finally
        {
            TryKill(child);
        }
    }

    [Fact]
    public void GetThreads_rejects_non_positive_pid()
        => Assert.Throws<ArgumentOutOfRangeException>(() => ProcessHelper.GetThreads(0));

    private static Process StartPing()
    {
        var info = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "ping.exe"),
            Arguments = "-n 20 127.0.0.1",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var process = Process.Start(info);
        Assert.NotNull(process);
        return process;
    }

    private static bool WaitForChild(int pid)
    {
        for (var i = 0; i < 20; i++)
        {
            if (ProcessHelper.Get(pid, ProcessDetailLevel.Identity) is not null)
                return true;
            Thread.Sleep(100);
        }
        return false;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch { }
    }
}
