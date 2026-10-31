using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PortScanProgressTests
{
    [Fact]
    public async Task Progress_reports_the_expected_probe_count()
    {
        var seen = new List<NetworkProgress>();
        var job = NetworkHelper.ScanPorts("192.0.2.1", PortSet.FromList([9, 80]), new PortScanOptions
        {
            ConnectTimeout = TimeSpan.FromMilliseconds(200),
            OverallTimeout = TimeSpan.FromSeconds(3)
        });
        job.ProgressChanged += (_, progress) => seen.Add(new NetworkProgress
        {
            Sent = progress.Sent,
            Received = progress.Received
        });

        await job.RunAsync();
        Assert.Contains(seen, progress => progress.Sent == 2);
        Assert.Contains(seen, progress => progress.Received == 2);
    }
}
