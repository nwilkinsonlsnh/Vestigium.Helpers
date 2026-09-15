using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessBacklogB3Tests
{
    [Fact]
    public void MaxMatches_over_cap_is_out_of_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessHelper.Search("a", ProcessSearchMode.Contains, maxResults: 5000));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessCampaign.Validate(new ProcessCampaignRecipe
        {
            Name = "cap",
            Query = "PID GT 0",
            MaxMatches = 300,
            Windows = [new ProcessCampaignWindow("m", new TimeOnly(8, 0), TimeSpan.FromMinutes(10), ProcessCampaignDays.All)]
        }));
    }

    [Fact]
    public void Thread_state_type_is_ProcessThreadState()
    {
        var threads = ProcessHelper.GetThreads(Environment.ProcessId);
        Assert.NotEmpty(threads);
        _ = threads[0].State;
        Assert.IsType<ProcessThreadState>(threads[0].State);
    }

    [Fact]
    public void Missing_pid_is_null_not_denied_row()
    {
        Assert.Null(ProcessHelper.Get(int.MaxValue - 7));
    }
}
