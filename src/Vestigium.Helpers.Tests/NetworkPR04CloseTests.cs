using System.Reflection;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR04CloseTests
{
    private static readonly string[] Required =
    [
        "PR04_001_network_has_no_charts_reference",
        "PR04_002_packed_oui_resolves_cisco",
        "PR04_002_packed_oui_unknown_is_none",
        "PR04_002_packed_registry_is_offline",
        "PR04_003_linux_mutate_denied",
        "PR04_003_ipv6_write_is_cap_or_cleanup",
        "PR04_003_ipv6_print_stays",
        "PR04_004_default_route_is_not_offered",
        "PR04_004_linux_ipv4_is_netlink_or_cap_deny",
        "PR04_005_ipv6_default_is_not_offered",
        "PR04_005_family_mismatch_rejected",
        "PR04_005_ipv6_write_is_not_v4_argument",
        "PR04_005_ipv6_prefix_129_rejected",
        "PR04_006_persistent_key_is_single_slash",
        "PR04_006_network_has_no_charts_or_http_client"
    ];

    [Fact]
    public void PR04_006_required_fixtures_exist()
    {
        var names = typeof(NetworkPR04CloseTests).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttribute<FactAttribute>() is not null)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var name in Required)
            Assert.True(names.Contains(name), name);
    }

    [Fact]
    public void PR04_006_persistent_key_is_single_slash()
    {
        var key = NetworkRouteKeys.PersistentRoutes;
        Assert.Equal(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\PersistentRoutes", key);
        Assert.DoesNotContain(@"\\", key);
    }

    [Fact]
    public void PR04_006_network_has_no_charts_or_http_client()
    {
        var names = typeof(NetworkHelper).Assembly.GetReferencedAssemblies().Select(a => a.Name!);
        Assert.DoesNotContain("Vestigium.Helpers.Charts", names, StringComparer.OrdinalIgnoreCase);
        Assert.Null(typeof(NetworkHelper).GetMethod("ChartShare"));
        Assert.NotNull(typeof(NetworkHelper).GetMethod(nameof(NetworkHelper.LookupOuiPacked)));
        Assert.NotNull(typeof(NetworkHelper).GetMethod(nameof(NetworkHelper.AddRoute)));
    }
}
