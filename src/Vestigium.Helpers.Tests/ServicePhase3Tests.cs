using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ServicePhase3Tests
{
    [Fact]
    public void List_visible_win32_never_marks_hidden()
    {
        var rows = ServiceHelper.List(ServiceDetailLevel.Identity, ServiceKind.Win32, ServiceListScope.Visible);
        Assert.NotEmpty(rows);
        Assert.All(rows, row => Assert.False(row.IsHidden));
        Assert.All(rows, row => Assert.Equal(ServiceKind.Win32, row.Kind));
    }

    [Fact]
    public void ListHidden_marks_hidden_and_is_disjoint_from_visible()
    {
        var visible = ServiceHelper.List(ServiceDetailLevel.Identity, ServiceKind.All, ServiceListScope.Visible);
        var hidden = ServiceHelper.ListHidden(ServiceDetailLevel.Identity, ServiceKind.All);
        var visibleNames = visible.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.NotNull(hidden);
        Assert.All(hidden, row => Assert.True(row.IsHidden));
        Assert.DoesNotContain(hidden, row => visibleNames.Contains(row.Name));
    }

    [Fact]
    public void List_all_is_union_of_visible_and_hidden()
    {
        var visible = ServiceHelper.List(ServiceDetailLevel.Identity, ServiceKind.All, ServiceListScope.Visible);
        var hidden = ServiceHelper.ListHidden(ServiceDetailLevel.Identity, ServiceKind.All);
        var all = ServiceHelper.List(ServiceDetailLevel.Identity, ServiceKind.All, ServiceListScope.All);

        Assert.True(all.Count >= visible.Count);
        Assert.Equal(visible.Count + hidden.Count, all.Count);
        Assert.Equal(hidden.Count, all.Count(r => r.IsHidden));
        Assert.Contains(all, row => string.Equals(row.Name, "EventLog", StringComparison.OrdinalIgnoreCase) && !row.IsHidden);
    }

    [Fact]
    public void List_drivers_visible_are_not_hidden()
    {
        var drivers = ServiceHelper.List(ServiceDetailLevel.Identity, ServiceKind.Driver, ServiceListScope.Visible);
        Assert.All(drivers, row => Assert.False(row.IsHidden));
        Assert.All(drivers, row => Assert.Equal(ServiceKind.Driver, row.Kind));
    }

    [Fact]
    public void GetDependsOn_eventlog_does_not_throw()
    {
        var deps = ServiceHelper.GetDependsOn("EventLog");
        Assert.NotNull(deps);
        Assert.All(deps, row => Assert.False(string.IsNullOrWhiteSpace(row.Name)));
    }

    [Fact]
    public void GetDependedBy_eventlog_does_not_throw()
    {
        var deps = ServiceHelper.GetDependedBy("EventLog");
        Assert.NotNull(deps);
    }

    [Fact]
    public void Dependency_tree_flatten_includes_root()
    {
        var tree = ServiceHelper.GetDependencyTree("EventLog", ServiceTreeDirection.DependsOn, ServiceDetailLevel.Slim);
        var flat = tree.Flatten();
        Assert.NotEmpty(flat);
        Assert.Equal("EventLog", flat[0].Name, ignoreCase: true);
        Assert.DoesNotContain(flat, row => string.IsNullOrWhiteSpace(row.Name));
    }

    [Fact]
    public void Dependency_tree_missing_throws()
        => Assert.Throws<InvalidOperationException>(() =>
            ServiceHelper.GetDependencyTree("NoSuchService_Vestigium"));

    [Fact]
    public void Both_directions_do_not_loop_forever()
    {
        var tree = ServiceHelper.GetDependencyTree("EventLog", ServiceTreeDirection.Both, ServiceDetailLevel.Slim);
        var flat = tree.Flatten();
        Assert.True(flat.Count <= ServiceHelper.MaxTreeNodes + 8, $"nodes={flat.Count}");
        Assert.Equal("EventLog", flat[0].Name, ignoreCase: true);
        var names = flat.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(names.Count, flat.Count);
    }
}
