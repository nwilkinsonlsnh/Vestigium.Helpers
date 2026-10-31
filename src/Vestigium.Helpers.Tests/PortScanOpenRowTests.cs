using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PortScanOpenRowTests
{
    [Fact]
    public void Rows_are_one_per_open_port()
    {
        var result = Sample();
        var rows = PortScanCsv.Rows(result);
        Assert.Equal(2, rows.Count);
        Assert.Equal("192.0.2.1", rows[0].Host);
        Assert.Equal(22, rows[0].Port);
        Assert.Equal(PortScanTransport.Tcp, rows[0].Transport);
        Assert.Equal("SSH-2.0-OpenSSH", rows[0].Banner);
        Assert.Equal(PortScanTransport.Udp, rows[1].Transport);
    }

    [Fact]
    public void Write_uses_the_same_rows_and_quotes_the_banner()
    {
        var csv = PortScanCsv.Write(Sample());
        Assert.StartsWith("host,port,transport,service,mss,banner", csv);
        Assert.Contains("192.0.2.1,22,tcp,ssh,1460,\"SSH-2.0-OpenSSH\"", csv);
        Assert.Contains("192.0.2.1,53,udp,dns,,\"\"", csv);
    }

    private static PortScanResult Sample()
        => new(
            "scan-test",
            NetworkJobStatus.Success,
            1,
            2,
            2,
            [],
            [
                new PortScanHostRow(
                    "192.0.2.1",
                    [new PortScanOpenPort(22, PortScanTransport.Tcp, "ssh", 1460, "SSH-2.0-OpenSSH", 12)],
                    [new PortScanOpenPort(53, PortScanTransport.Udp, "dns", null, null, 4)],
                    null)
            ],
            []);
}
