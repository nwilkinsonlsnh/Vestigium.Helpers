using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class TlsProbeTests
{
    [Fact]
    public void Options_default_to_five_seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(5), new TlsProbeOptions().Timeout);
    }

    [Fact]
    public void Port_out_of_range_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.ProbeTls("192.0.2.1", 0));
    }
}
