using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PortScanCsvTests
{
    [Fact]
    public void Csv_is_one_row_per_open_port_and_quotes_the_banner()
    {
        var result = new PortScanResult(
            "scan-test",
            NetworkJobStatus.Success,
            1,
            2,
            1,
            [],
            [
                new PortScanHostRow(
                    "192.0.2.1",
                    [new PortScanOpenPort(22, PortScanTransport.Tcp, "ssh", 1460, "SSH-2.0-OpenSSH", 12)],
                    [],
                    null)
            ],
            []);

        var csv = PortScanCsv.Write(result);
        Assert.StartsWith("host,port,transport,service,mss,banner", csv);
        Assert.Contains("192.0.2.1,22,tcp,ssh,1460,\"SSH-2.0-OpenSSH\"", csv);
    }
}
