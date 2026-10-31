using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PortScanRollupTests
{
    [Fact]
    public void Open_ports_are_named_and_split_by_transport()
    {
        var hits = new[]
        {
            Hit("lab", 443, PortScanTransport.Tcp, PortScanStatus.Open, 1460, null),
            Hit("lab", 53, PortScanTransport.Udp, PortScanStatus.Open, null, null),
            Hit("lab", 22, PortScanTransport.Tcp, PortScanStatus.Closed, null, null)
        };

        var rows = PortScanRollup.Hosts(hits, infer: false);
        Assert.Single(rows);
        Assert.Equal("https", rows[0].OpenTcp[0].Service);
        Assert.Equal(1460, rows[0].OpenTcp[0].Mss);
        Assert.Equal("dns", rows[0].OpenUdp[0].Service);
        Assert.Null(rows[0].Inference);
    }

    [Fact]
    public void Inference_is_optional_and_uses_open_ports_only()
    {
        var hits = new[]
        {
            Hit("lab", 445, PortScanTransport.Tcp, PortScanStatus.Open, null, null),
            Hit("lab", 3389, PortScanTransport.Tcp, PortScanStatus.Open, null, null),
            Hit("lab", 135, PortScanTransport.Tcp, PortScanStatus.Open, null, null)
        };

        var off = PortScanRollup.Hosts(hits, infer: false);
        var on = PortScanRollup.Hosts(hits, infer: true);
        Assert.Null(off[0].Inference);
        Assert.Equal(InferredDeviceClass.WindowsHost, on[0].Inference!.DeviceClass);
        Assert.Equal(InferenceConfidence.High, on[0].Inference.Confidence);
    }

    private static PortScanHit Hit(string host, int port, PortScanTransport transport, PortScanStatus status, int? mss, string? banner)
        => new(host, port, transport, status, host, 1, null, mss, banner);
}
