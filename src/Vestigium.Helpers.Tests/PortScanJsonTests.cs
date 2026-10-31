using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PortScanJsonTests
{
    [Fact]
    public void Write_is_the_open_rows_and_does_not_log()
    {
        var json = PortScanJson.Write(Sample());
        Assert.Contains("\"host\": \"192.0.2.1\"", json);
        Assert.Contains("\"port\": 22", json);
        Assert.Contains("\"banner\": \"SSH-2.0\"", json);
        Assert.DoesNotContain("80", json);
    }

    private static PortScanResult Sample()
        => new(
            "scan-test",
            NetworkJobStatus.Success,
            1,
            2,
            1,
            [
                new PortScanHit("192.0.2.1", 22, PortScanTransport.Tcp, PortScanStatus.Open, "192.0.2.1", 12, null, Banner: "SSH-2.0"),
                new PortScanHit("192.0.2.1", 80, PortScanTransport.Tcp, PortScanStatus.Closed, "192.0.2.1", 4, null)
            ],
            [
                new PortScanHostRow(
                    "192.0.2.1",
                    [new PortScanOpenPort(22, PortScanTransport.Tcp, "ssh", null, "SSH-2.0", 12)],
                    [],
                    null)
            ],
            []);
}
