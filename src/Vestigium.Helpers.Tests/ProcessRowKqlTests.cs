using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessRowKqlTests
{
    [Fact]
    public void SearchThreads_by_tid_returns_that_thread()
    {
        var threads = ProcessHelper.GetThreads(Environment.ProcessId);
        Assert.NotEmpty(threads);
        var tid = threads[0].ThreadId;
        var hits = ProcessHelper.SearchThreads(Environment.ProcessId, "TID == " + tid);
        Assert.Contains(hits, row => row.ThreadId == tid);
    }

    [Fact]
    public void MatchSystem_process_count_is_true()
        => Assert.True(ProcessHelper.MatchSystem("SYS.ProcessCount GT 0"));
}
