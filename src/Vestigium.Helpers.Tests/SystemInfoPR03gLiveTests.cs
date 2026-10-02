using Vestigium.Helpers.SystemInfo.Cpu;
using MemoryDoor = Vestigium.Helpers.SystemInfo.Memory.MemoryFacts;

namespace Vestigium.Helpers.Tests;

public sealed class SystemInfoPR03gLiveTests
{
    [Fact]
    public void Cpu_host_returns_topology_counts()
    {
        var fact = CpuFacts.Host;
        Assert.True(fact.IsOk, "topology was Unavailable");
        var host = fact.Value;
        Assert.True(host.Sockets > 0);
        Assert.True(host.Cores > 0);
        Assert.True(host.Logical > 0);
        Assert.True(host.Logical >= host.Cores);
    }

    [Fact]
    public void Cpu_live_returns_census_and_mhz()
    {
        var fact = CpuFacts.Live();
        Assert.True(fact.IsOk, "live census was Unavailable");
        var live = fact.Value;
        Assert.True(live.Processes > 0);
        Assert.True(live.Threads >= live.Processes);
        Assert.True(live.Handles > 0);
        Assert.True(live.MaxMhz > 0);
        Assert.True(live.CurrentMhz > 0);
    }

    [Fact]
    public void Memory_read_returns_bytes()
    {
        var read = MemoryDoor.Read();
        Assert.True(read.TotalBytes.IsOk, "physical total was Unavailable");
        Assert.True(read.AvailableBytes.IsOk);
        Assert.True(read.InUseBytes.IsOk);
        Assert.True(read.TotalBytes.Value > 0);
        Assert.True(read.AvailableBytes.Value <= read.TotalBytes.Value);
        Assert.True(read.CommitPeakBytes.IsOk, "commit peak was Unavailable");
        Assert.True(read.PagedBytes.IsOk);
        Assert.True(read.NonpagedBytes.IsOk);
        Assert.True(read.CommitPeakBytes.Value > 0);
    }
}
