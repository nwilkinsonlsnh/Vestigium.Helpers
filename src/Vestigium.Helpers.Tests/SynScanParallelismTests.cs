using Vestigium.Helpers.Network;
using Vestigium.Helpers.Network.RawSocket;

namespace Vestigium.Helpers.Tests;

public sealed class SynScanParallelismTests
{
    [Fact]
    public void Default_parallelism_is_eight()
    {
        Assert.Equal(8, new SynScanOptions().Parallelism);
        Assert.Equal(32, SynScanOptions.MaxParallelism);
    }

    [Fact]
    public void Over_max_throws()
    {
        var options = new SynScanOptions { Parallelism = 33 };
        Assert.Throws<ArgumentOutOfRangeException>(() => RawSocketHelper.SynScan("192.0.2.1", PortSet.FromList([22]), options));
    }
}
