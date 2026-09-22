using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ServiceHelperTests
{
    [Fact]
    public void Probe_returns_identity()
        => Assert.Equal(ServiceHelper.Identity, ServiceHelper.Probe());

    [Fact]
    public void List_visible_win32_is_not_empty()
    {
        var rows = ServiceHelper.List();
        Assert.NotEmpty(rows);
        Assert.Contains(rows, row => string.Equals(row.Name, "EventLog", StringComparison.OrdinalIgnoreCase));
        Assert.All(rows, row => Assert.False(row.IsHidden));
    }

    [Fact]
    public void ListHidden_does_not_throw_and_marks_hidden()
    {
        var hidden = ServiceHelper.ListHidden();
        Assert.NotNull(hidden);
        Assert.All(hidden, row => Assert.True(row.IsHidden));
    }

    [Fact]
    public void Get_event_log_fills_identity()
    {
        var row = ServiceHelper.Get("EventLog");
        Assert.NotNull(row);
        Assert.Equal("EventLog", row!.Name, ignoreCase: true);
        Assert.False(string.IsNullOrWhiteSpace(row.DisplayName));
        Assert.NotEqual(ServiceStatus.Unknown, row.Status);
    }

    [Fact]
    public void TryGet_blank_is_false()
        => Assert.False(ServiceHelper.TryGet(" ", out _));

    [Fact]
    public void Search_contains_spooler()
    {
        var hits = ServiceHelper.Search("spool", ServiceSearchMode.Contains);
        Assert.Contains(hits, row => row.Name.Contains("spool", StringComparison.OrdinalIgnoreCase)
            || (row.DisplayName?.Contains("spool", StringComparison.OrdinalIgnoreCase) ?? false));
    }

    [Fact]
    public void Search_rejects_over_cap()
        => Assert.Throws<ArgumentException>(() => ServiceHelper.Search("e", ServiceSearchMode.Contains, maxResults: 300));

    [Fact]
    public void SetStartType_without_confirm_is_denied()
        => Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.SetStartType("EventLog", ServiceStartType.Automatic, confirm: false).Status);

    [Fact]
    public void SetLogon_without_confirm_is_denied()
        => Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.SetLogon("EventLog", new ServiceLogonRequest { Confirm = false }).Status);

    [Fact]
    public void SetRecovery_without_confirm_is_denied()
        => Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.SetRecovery("EventLog", new ServiceRecoveryRequest { Confirm = false }).Status);

    [Fact]
    public void Watch_interval_out_of_range()
        => Assert.Throws<ArgumentOutOfRangeException>(() => ServiceHelper.Watch("EventLog", TimeSpan.FromMilliseconds(10)));
}
