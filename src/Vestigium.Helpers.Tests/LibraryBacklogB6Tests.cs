using Vestigium.Helpers.Network;
using Vestigium.Helpers.Processes;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class LibraryBacklogB6Tests
{
    [Fact]
    public void DataUnit_megabit_is_mb()
    {
        Assert.Equal(DataUnit.Mb, DataUnit.Megabit);
        Assert.Equal(DataUnit.MB, DataUnit.Megabyte);
        Assert.Equal(DataUnit.Gb, DataUnit.Gigabit);
    }

    [Fact]
    public void Logging_has_warning()
        => Assert.True(Enum.IsDefined(VestigiumStatus.Warning));

    [Fact]
    public void Thread_state_is_defined()
    {
        var threads = ProcessHelper.GetThreads(Environment.ProcessId);
        Assert.NotEmpty(threads);
        Assert.True(Enum.IsDefined(threads[0].State));
    }
}
