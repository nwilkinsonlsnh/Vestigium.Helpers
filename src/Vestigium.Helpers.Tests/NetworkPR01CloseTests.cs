using System.Reflection;
using Xunit;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR01CloseTests
{
    static readonly string[] Required =
    [
        "PR01_001_custom_registry_http_is_rejected",
        "PR01_001_custom_registry_without_allow_is_rejected",
        "PR01_001_loopback_https_is_rejected",
        "PR01_001_redirect_is_not_followed",
        "PR01_001_parse_mac_does_not_http",
        "PR01_002_hooks_are_not_public",
        "PR01_003_continuous_without_duration_gets_default_cap",
        "PR01_003_zero_interval_rejected_without_burst",
        "PR01_004_udp_foreign_source_discarded",
        "PR01_005_missing_interface_does_not_use_index_1",
        "PR01_006_results_path_escape_rejected",
        "PR01_008_huge_oui_file_rejected",
        "PR01_009_directory_prefix_is_stripped",
        "PR01_010_persistent_delete_access_denied_is_typed"
    ];

    [Fact]
    public void PR01_011_required_fixtures_exist()
    {
        var names = typeof(NetworkPR01CloseTests).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttributes().Any(a => a.GetType().Name is "FactAttribute" or "TheoryAttribute"))
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(Required, name => Assert.True(names.Contains(name), name + " is missing"));
    }
}
