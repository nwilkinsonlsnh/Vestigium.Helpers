using System.Reflection;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkCloseTests
{
    private static readonly string[] Required =
    [
        "Trace_engine_finish_calls_logfinished",
        "LogFinished_failed_and_timedout_use_operation_failed",
        "Ipv6_write_does_not_borrow_ipv4_interface_index",
        "Missing_ipv6_interface_does_not_use_index_1",
        "Catalog_register_includes_share_stats_progress",
        "Recipe_round_trips_echo_options",
        "Old_recipe_without_echo_opens_with_defaults",
        "Named_events_14530_14535_14540_exist",
        "Network_assembly_has_no_charts"
    ];

    [Fact]
    public void Required_network_fixtures_exist()
    {
        var names = typeof(NetworkCloseTests).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttribute<FactAttribute>() is not null)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var name in Required)
            Assert.True(names.Contains(name), name);
    }

    [Fact]
    public void Network_assembly_has_no_charts()
    {
        var names = typeof(NetworkHelper).Assembly.GetReferencedAssemblies().Select(a => a.Name!);
        Assert.DoesNotContain("Vestigium.Helpers.Charts", names, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("ScottPlot", names, StringComparer.OrdinalIgnoreCase);
        Assert.Null(typeof(NetworkHelper).GetMethod("ChartShare"));
        Assert.Null(typeof(NetworkHelper).GetMethod("Plot"));
    }
}
