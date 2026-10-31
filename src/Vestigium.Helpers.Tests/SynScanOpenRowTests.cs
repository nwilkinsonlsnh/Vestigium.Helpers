using Vestigium.Helpers.Network.RawSocket;

namespace Vestigium.Helpers.Tests;

public sealed class SynScanOpenRowTests
{
    [Fact]
    public void Rows_are_open_ports_only()
    {
        var rows = SynScanCsv.Rows(Sample());
        Assert.Single(rows);
        Assert.Equal("192.0.2.1", rows[0].Host);
        Assert.Equal(22, rows[0].Port);
        Assert.Equal(12, rows[0].ElapsedMs);
    }

    [Fact]
    public void Write_uses_the_same_rows_and_quotes_the_detail()
    {
        var csv = SynScanCsv.Write(Sample());
        Assert.StartsWith("host,port,elapsed,detail", csv);
        Assert.Contains("192.0.2.1,22,12,\"open\"", csv);
        Assert.DoesNotContain("80,", csv);
    }

    private static SynScanResult Sample()
        => new(
            "syn-test",
            SynScanStatus.Success,
            null,
            1,
            2,
            1,
            [
                new SynScanHit("192.0.2.1", 22, SynPortStatus.Open, 12, "open"),
                new SynScanHit("192.0.2.1", 80, SynPortStatus.Closed, 4, "closed")
            ]);
}
