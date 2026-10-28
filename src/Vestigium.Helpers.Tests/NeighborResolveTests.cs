using System.Net;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NeighborResolveTests
{
    [Fact]
    public void FormatMac_rejects_empty_and_short()
    {
        Assert.Null(NeighborResolve.FormatMac(null, 6));
        Assert.Null(NeighborResolve.FormatMac(new byte[6], 6));
        Assert.Null(NeighborResolve.FormatMac([1, 2, 3, 4, 5, 6], 5));
        Assert.Equal("01:02:03:04:05:06", NeighborResolve.FormatMac([1, 2, 3, 4, 5, 6], 6));
    }

    [Fact]
    public void Probe_does_not_throw_on_loopback()
    {
        var result = NetworkHelper.ProbeNeighbor(IPAddress.Loopback.ToString());
        Assert.Equal("127.0.0.1", result.Address);
    }
}
