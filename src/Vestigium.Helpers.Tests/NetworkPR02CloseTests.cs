using System.Reflection;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR02CloseTests
{
    static readonly string[] Required =
    [
        "PR02_002_linux_add_route_is_network_route_denied",
        "PR02_002_windows_add_route_without_admin_is_denied",
        "PR02_003_label_over_63_rejected",
        "PR02_003_name_over_255_rejected",
        "PR02_004_two_appends_are_whole_lines",
        "PR02_005_linux_forbidden_stays_typed",
        "PR02_006_ipv6_destination_rejected_on_add",
        "PR02_007_empty_samples_throw",
        "PR02_007_empty_series_throw"
    ];

    [Fact]
    public void PR02_008_required_fixtures_exist()
    {
        var names = typeof(NetworkPR02CloseTests).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttributes().Any(a => a.GetType().Name is "FactAttribute" or "TheoryAttribute"))
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(Required, name => Assert.True(names.Contains(name), name + " is missing"));
    }
}
