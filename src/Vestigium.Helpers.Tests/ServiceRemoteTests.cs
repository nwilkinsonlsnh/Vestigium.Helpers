using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ServiceRemoteTests
{
    [Fact]
    public void Local_aliases_do_not_set_machine()
    {
        Assert.True(ServiceHelper.For(".").IsLocal);
        Assert.True(ServiceHelper.For("localhost").IsLocal);
        Assert.True(ServiceHelper.For(Environment.MachineName).IsLocal);
        Assert.True(ServiceHelper.Local.IsLocal);
        Assert.Null(ServiceHelper.For(".").Machine);
    }

    [Fact]
    public void For_remote_name_keeps_machine()
    {
        var client = ServiceHelper.For("VESTIGIUM-TEST-BOX");
        Assert.False(client.IsLocal);
        Assert.Equal("VESTIGIUM-TEST-BOX", client.Machine);
    }

    [Fact]
    public void CanConnect_local_is_true()
        => Assert.True(ServiceHelper.CanConnect("."));

    [Fact]
    public void CanConnect_missing_host_is_false()
        => Assert.False(ServiceHelper.CanConnect("no-such-host-vestigium-xyz"));

    [Fact]
    public void For_dot_lists_eventlog()
    {
        var rows = ServiceHelper.For(".").List(ServiceDetailLevel.Identity);
        Assert.Contains(rows, row => string.Equals(row.Name, "EventLog", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void For_dot_get_eventlog()
    {
        var row = ServiceHelper.For(".").Get("EventLog", ServiceDetailLevel.Slim, joinProcess: false);
        Assert.NotNull(row);
        Assert.Equal("EventLog", row!.Name, ignoreCase: true);
        Assert.True(string.IsNullOrWhiteSpace(row.Machine));
    }

    [Fact]
    public void Missing_host_list_is_empty()
        => Assert.Empty(ServiceHelper.For("no-such-host-vestigium-xyz").List(ServiceDetailLevel.Identity));

    [Fact]
    public void Missing_host_get_is_null()
        => Assert.Null(ServiceHelper.For("no-such-host-vestigium-xyz").Get("EventLog", ServiceDetailLevel.Identity, joinProcess: false));

    [Fact]
    public void For_blank_throws()
        => Assert.Throws<ArgumentException>(() => ServiceHelper.For(" "));
}
