using System.Net.NetworkInformation;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPr02IcmpTests
{
    [Fact]
    public void PR02_005_linux_forbidden_stays_typed()
    {
        Assert.True(IcmpEchoEngine.IsForbidden(new PlatformNotSupportedException("icmp")));
        Assert.True(IcmpEchoEngine.IsForbidden(new PingException("wrap", new InvalidOperationException("Operation not permitted"))));
        Assert.True(IcmpEchoEngine.IsForbidden(new InvalidOperationException("Access denied")));
        Assert.False(IcmpEchoEngine.IsForbidden(new InvalidOperationException("timeout")));
        Assert.Equal(NetworkJobStatus.Failed, IcmpEchoEngine.DecideStatus(false, 4, 4, 0, protocolForbidden: true));
    }
}
