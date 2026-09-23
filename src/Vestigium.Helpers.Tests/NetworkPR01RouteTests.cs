using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPr01RouteTests
{
    [Fact]
    public void PR01_005_missing_interface_does_not_use_index_1()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            NetworkRouteMutation.ResolveInterfaceIndex(null, null));
        Assert.Contains("InterfaceIndex", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch("guess", ex.Message);

        Assert.Equal(12, NetworkRouteMutation.ResolveInterfaceIndex(12, null));
        Assert.Equal(4, NetworkRouteMutation.ResolveInterfaceIndex(null, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkRouteMutation.ResolveInterfaceIndex(0, 1));
    }
}
