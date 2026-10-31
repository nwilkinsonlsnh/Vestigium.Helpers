using Vestigium.Helpers.Network;
using Vestigium.Helpers.Network.RawSocket;

namespace Vestigium.Helpers.Tests;

public sealed class SynScanProgressTests
{
    [Fact]
    public async Task Progress_reports_expected_probes_before_the_result()
    {
        var seen = new List<NetworkProgress>();
        var job = RawSocketHelper.SynScan("192.0.2.1", PortSet.FromList([22, 80, 443]));
        job.ProgressChanged += (_, progress) => seen.Add(new NetworkProgress
        {
            Sent = progress.Sent,
            Received = progress.Received
        });

        var result = await job.RunAsync();

        Assert.NotEmpty(seen);
        Assert.Equal(3, seen[0].Sent);
        Assert.Equal(0, seen[0].Received);
        Assert.True(result.Status is SynScanStatus.Denied or SynScanStatus.Success or SynScanStatus.TimedOut);
    }
}
