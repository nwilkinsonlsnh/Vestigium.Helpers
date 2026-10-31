using Vestigium.Helpers.Network.RawSocket;

namespace Vestigium.Helpers.Tests;

public sealed class RawSocketRightsTests
{
    [Fact]
    public void Check_returns_a_typed_result()
    {
        var access = RawSocketRights.Check();
        Assert.Contains(access.Privilege, new[] { RawSocketPrivilege.Granted, RawSocketPrivilege.Denied });
        Assert.False(string.IsNullOrWhiteSpace(access.Detail));
    }

    [Fact]
    public void Cap_net_raw_is_bit_13()
    {
        Assert.Equal(13, RawSocketRights.CapNetRaw);
    }
}
