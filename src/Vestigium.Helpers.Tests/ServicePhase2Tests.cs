using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ServicePhase2Tests
{
    [Fact]
    public void Identity_does_not_fill_config()
    {
        var row = ServiceHelper.Get("EventLog", ServiceDetailLevel.Identity, joinProcess: false);
        Assert.NotNull(row);
        Assert.Null(row!.ImagePath);
        Assert.Null(row.Account);
        Assert.Null(row.StartType);
        Assert.Empty(row.FailureActions);
    }

    [Fact]
    public void Slim_fills_start_type_image_and_account()
    {
        var row = ServiceHelper.Get("EventLog", ServiceDetailLevel.Slim, joinProcess: false);
        Assert.NotNull(row);
        Assert.False(string.IsNullOrWhiteSpace(row!.ImagePath));
        Assert.False(string.IsNullOrWhiteSpace(row.Account));
        Assert.NotNull(row.StartType);
        Assert.Empty(row.FailureActions);
    }

    [Fact]
    public void Full_eventlog_has_config_account_and_failure_list()
    {
        var row = ServiceHelper.Get("EventLog", ServiceDetailLevel.Full, joinProcess: false);
        Assert.NotNull(row);
        Assert.Equal("EventLog", row!.Name, ignoreCase: true);
        Assert.False(string.IsNullOrWhiteSpace(row.ImagePath));
        Assert.Contains("svchost", row.ImagePath!, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(row.Account));
        Assert.Equal("LocalSystem", row.Account, ignoreCase: true);
        Assert.NotNull(row.StartType);
        Assert.NotNull(row.FailureActions);
        Assert.NotNull(row.DelayedAutoStart);
        Assert.False(string.IsNullOrWhiteSpace(row.Description));
    }

    [Fact]
    public void Full_row_has_no_password_property()
        => Assert.DoesNotContain(
            typeof(ServiceInfo).GetProperties(),
            p => string.Equals(p.Name, "Password", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void Delayed_automatic_maps_to_automatic_delayed_when_flag_set()
    {
        var rows = ServiceHelper.List(ServiceDetailLevel.Slim, ServiceKind.Win32, ServiceListScope.Visible);
        var delayed = rows.Where(r => r.StartType == ServiceStartType.AutomaticDelayed).ToArray();
        Assert.All(delayed, row => Assert.True(row.DelayedAutoStart == true));
    }

    [Fact]
    public void Search_image_path_windows_hits()
    {
        var hits = ServiceHelper.Search(
            "windows",
            ServiceSearchMode.Contains,
            ServiceSearchFields.ImagePath,
            ServiceDetailLevel.Slim);
        Assert.NotEmpty(hits);
        Assert.All(hits, row => Assert.Contains("windows", row.ImagePath ?? string.Empty, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Search_account_localsystem_hits()
    {
        var hits = ServiceHelper.Search(
            "LocalSystem",
            ServiceSearchMode.Contains,
            ServiceSearchFields.Account,
            ServiceDetailLevel.Slim);
        Assert.NotEmpty(hits);
        Assert.All(hits, row => Assert.Contains("LocalSystem", row.Account ?? string.Empty, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Denied_config_is_recorded_not_thrown()
    {
        var row = ServiceHelper.Get("EventLog", ServiceDetailLevel.Full, joinProcess: false);
        Assert.NotNull(row);
        Assert.DoesNotContain(row!.Availability, a => a.State == "Thrown");
    }
}
