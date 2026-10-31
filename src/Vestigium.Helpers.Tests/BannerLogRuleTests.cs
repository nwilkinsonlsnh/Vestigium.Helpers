using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class BannerLogRuleTests
{
    [Fact]
    public void A_banner_value_is_reduced_to_set()
    {
        var line = NetworkLog.Redact("Connected tcpConnect job=tcp-1 banner=SSH-2.0-OpenSSH");
        Assert.Equal("Connected tcpConnect job=tcp-1 banner=set", line);
        Assert.DoesNotContain("OpenSSH", line);
    }

    [Fact]
    public void A_null_banner_stays_null()
    {
        Assert.Equal("banner=null", NetworkLog.Redact("banner=null"));
    }
}
