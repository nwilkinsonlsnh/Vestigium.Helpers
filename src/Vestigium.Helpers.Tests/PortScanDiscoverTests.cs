using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PortScanDiscoverTests
{
    [Fact]
    public void Discovery_is_off_by_default()
    {
        var options = new PortScanOptions();
        Assert.False(options.Discover);
        Assert.Null(options.DiscoverPort);
    }

    [Fact]
    public async Task Silent_host_is_skipped_when_discovery_is_on()
    {
        var job = NetworkHelper.ScanPorts("192.0.2.1", PortSet.FromList([80]), new PortScanOptions
        {
            Discover = true,
            ConnectTimeout = TimeSpan.FromMilliseconds(200),
            OverallTimeout = TimeSpan.FromSeconds(3)
        });
        var result = await job.RunAsync();
        Assert.Contains("192.0.2.1", result.Skipped);
        Assert.Empty(result.Hits);
    }
}
