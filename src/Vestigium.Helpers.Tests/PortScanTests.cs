using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PortScanTests
{
    [Fact]
    public void Kind_is_portScan()
    {
        var job = NetworkHelper.ScanPorts("127.0.0.1", PortSet.FromList([1]), new PortScanOptions { ConnectTimeout = TimeSpan.FromMilliseconds(200), OverallTimeout = TimeSpan.FromSeconds(2) });
        Assert.Equal("portScan", job.Kind);
    }

    [Fact]
    public void Empty_hosts_or_ports_throw()
    {
        Assert.Throws<ArgumentException>(() => NetworkHelper.ScanPorts(Array.Empty<string>(), PortSet.FromList([80])));
        Assert.Throws<ArgumentException>(() => NetworkHelper.ScanPorts("127.0.0.1", PortSet.FromList([])));
    }

    [Fact]
    public async Task Loopback_closed_port_is_closed_or_timed_out()
    {
        var job = NetworkHelper.ScanPorts("127.0.0.1", PortSet.FromList([1]), new PortScanOptions
        {
            ConnectTimeout = TimeSpan.FromMilliseconds(300),
            OverallTimeout = TimeSpan.FromSeconds(3),
            Parallelism = 2
        });
        var result = await job.RunAsync();
        Assert.Equal(1, result.HostCount);
        Assert.Equal(1, result.PortCount);
        Assert.Single(result.Hits);
        Assert.Contains(result.Hits[0].Status, new[] { PortScanStatus.Closed, PortScanStatus.TimedOut, PortScanStatus.Open });
    }
}
