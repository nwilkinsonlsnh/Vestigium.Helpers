using Vestigium.Helpers.Network;
using Vestigium.Helpers.Network.RawSocket;

namespace Vestigium.Helpers.Tests;

public sealed class SynScanBindTests
{
    [Fact]
    public void Default_pin_is_unset()
    {
        var options = new SynScanOptions();
        Assert.Equal(0, options.InterfaceIndex);
        Assert.Null(options.SourceAddress);
    }

    [Fact]
    public void Negative_index_throws()
    {
        var options = new SynScanOptions { InterfaceIndex = -1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => RawSocketHelper.SynScan("192.0.2.1", PortSet.FromList([22]), options));
    }
}
