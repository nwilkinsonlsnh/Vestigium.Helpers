using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PortScanMacTests
{
    [Fact]
    public void ResolveMac_defaults_off()
    {
        Assert.False(new PortScanOptions().ResolveMac);
    }

    [Fact]
    public void Host_row_mac_is_null_when_not_supplied()
    {
        var rows = PortScanRollup.Hosts([Hit()], infer: false);
        Assert.Single(rows);
        Assert.Null(rows[0].Mac);
    }

    [Fact]
    public void Host_row_takes_a_supplied_mac()
    {
        var macs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["192.0.2.1"] = "AA:BB:CC:DD:EE:FF"
        };
        var rows = PortScanRollup.Hosts([Hit()], infer: false, macs);
        Assert.Equal("AA:BB:CC:DD:EE:FF", rows[0].Mac);
    }

    private static PortScanHit Hit()
        => new("192.0.2.1", 22, PortScanTransport.Tcp, PortScanStatus.Open, "192.0.2.1", 4, null);
}
