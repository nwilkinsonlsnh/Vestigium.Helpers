using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class DnsProbeTests
{
    [Fact]
    public void Kind_is_probeDns()
    {
        var job = NetworkHelper.ProbeDns("localhost", new DnsLookupOptions { Timeout = TimeSpan.FromMilliseconds(200) });
        Assert.Equal("probeDns", job.Kind);
    }

    [Fact]
    public void Timeout_rcode_is_timed_out()
    {
        Assert.Equal(DnsProbeStatus.TimedOut, DnsProbeEngine.Map(DnsRcode.Timeout));
        Assert.Equal(DnsProbeStatus.Refused, DnsProbeEngine.Map(DnsRcode.Refused));
        Assert.Equal(DnsProbeStatus.Answered, DnsProbeEngine.Map(DnsRcode.NxDomain));
        Assert.Equal(DnsProbeStatus.Answered, DnsProbeEngine.Map(DnsRcode.NoError));
    }
}
