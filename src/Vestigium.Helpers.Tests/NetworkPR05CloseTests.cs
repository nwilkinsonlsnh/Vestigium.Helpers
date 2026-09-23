using System.Reflection;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPr05CloseTests
{
    private static readonly string[] Required =
    [
        "PR05_001_persist_key_has_single_separators",
        "PR05_001_mutation_uses_route_keys",
        "PR05_006_old_ipv6_reject_names_are_gone"
    ];

    [Fact]
    public void PR05_007_required_fixtures_exist()
    {
        var names = typeof(NetworkPr05CloseTests).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttribute<FactAttribute>() is not null)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var name in Required)
            Assert.True(names.Contains(name), name);
    }

    [Fact]
    public void PR05_007_network_still_has_no_charts()
    {
        var names = typeof(NetworkHelper).Assembly.GetReferencedAssemblies().Select(a => a.Name!);
        Assert.DoesNotContain("Vestigium.Helpers.Charts", names, StringComparer.OrdinalIgnoreCase);
    }
}
