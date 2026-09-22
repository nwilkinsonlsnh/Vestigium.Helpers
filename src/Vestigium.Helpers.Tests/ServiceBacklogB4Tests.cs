using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ServiceBacklogB4Tests
{
    [Fact]
    public void Tree_cap_is_unique_names_and_matches_helper()
    {
        Assert.Equal(256, ServiceHelper.MaxTreeNodes);
        Assert.Equal(ServiceHelper.MaxTreeNodes, ServiceTreeWalker.MaxNodes);

        foreach (var direction in new[] { ServiceTreeDirection.DependsOn, ServiceTreeDirection.DependedBy, ServiceTreeDirection.Both })
        {
            var flat = ServiceHelper.GetDependencyTree("EventLog", direction, ServiceDetailLevel.Identity).Flatten();
            var names = flat.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Equal(names.Count, flat.Count);
            Assert.True(flat.Count <= ServiceHelper.MaxTreeNodes);
        }
    }
}
