using System.Reflection;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR05PersistTests
{
    [Fact]
    public void PR05_001_persist_key_has_single_separators()
    {
        var key = NetworkRouteKeys.PersistentRoutes;
        Assert.Equal(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\PersistentRoutes", key);
        Assert.DoesNotContain(@"\\", key);
        Assert.StartsWith(@"SYSTEM\CurrentControlSet\", key, StringComparison.Ordinal);
    }

    [Fact]
    public void PR05_001_mutation_uses_route_keys()
    {
        var leftover = typeof(NetworkRouteMutation).GetField("PersistentKey", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.Null(leftover);
    }
}
