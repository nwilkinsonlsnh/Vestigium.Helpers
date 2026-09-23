using System.Security;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPr01PersistentTests
{
    [Fact]
    public void PR01_010_persistent_delete_access_denied_is_typed()
    {
        var denied = NetworkRouteMutation.PersistentAccessDenied(new UnauthorizedAccessException("denied"));
        Assert.IsType<NetworkRouteDenied>(denied);
        Assert.Contains("PersistentRoutes", denied.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<UnauthorizedAccessException>(denied.InnerException);

        var security = NetworkRouteMutation.PersistentAccessDenied(new SecurityException("acl"));
        Assert.IsType<NetworkRouteDenied>(security);
        Assert.IsType<SecurityException>(security.InnerException);
    }
}
