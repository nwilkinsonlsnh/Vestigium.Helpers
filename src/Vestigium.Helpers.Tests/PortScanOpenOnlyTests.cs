using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PortScanOpenOnlyTests
{
    [Fact]
    public void OpenOnly_is_off_by_default()
    {
        Assert.False(new PortScanOptions().OpenOnly);
    }

    [Fact]
    public async Task OpenOnly_drops_non_open_hits()
    {
        var job = NetworkHelper.ScanPorts("192.0.2.1", PortSet.FromList([9, 80]), new PortScanOptions
        {
            OpenOnly = true,
            ConnectTimeout = TimeSpan.FromMilliseconds(200),
            OverallTimeout = TimeSpan.FromSeconds(3)
        });
        var result = await job.RunAsync();
        Assert.Empty(result.Hits);
        Assert.Equal(0, result.OpenCount);
    }
}
