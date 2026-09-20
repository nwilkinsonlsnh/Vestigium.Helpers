using System.Reflection;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR05RenameTests
{
    [Fact]
    public void PR05_006_old_ipv6_reject_names_are_gone()
    {
        var names = typeof(NetworkPR05RenameTests).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttribute<FactAttribute>() is not null)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("PR02_006_ipv6_destination_rejected_on_add", names);
        Assert.DoesNotContain("PR04_003_ipv6_write_denied", names);
        Assert.Contains("PR02_006_ipv6_write_is_cap_or_cleanup", names);
        Assert.Contains("PR04_003_ipv6_write_is_cap_or_cleanup", names);
    }
}
