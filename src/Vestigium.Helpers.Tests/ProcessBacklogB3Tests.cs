using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessBacklogB3Tests
{
    [Fact]
    public void MaxMatches_over_cap_is_out_of_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessHelper.Search("a", ProcessSearchMode.Contains, maxResults: 5000));
    }

    [Fact]
    public void Thread_state_is_defined()
    {
        var threads = ProcessHelper.GetThreads(Environment.ProcessId);
        Assert.NotEmpty(threads);
        Assert.True(Enum.IsDefined(threads[0].State));
    }

    [Fact]
    public void Missing_pid_is_null_not_denied_row()
    {
        Assert.Null(ProcessHelper.Get(int.MaxValue - 7));
    }
}
