using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ServicePhase1Tests
{
    [Fact]
    public void Probe_returns_identity_and_does_not_throw()
        => Assert.Equal("Vestigium.Helpers.Services", ServiceHelper.Probe());

    [Fact]
    public void List_default_is_visible_win32_and_includes_eventlog()
    {
        var rows = ServiceHelper.List();
        Assert.NotEmpty(rows);
        Assert.Contains(rows, row => string.Equals(row.Name, "EventLog", StringComparison.OrdinalIgnoreCase));
        Assert.All(rows, row => Assert.False(row.IsHidden));
        Assert.All(rows, row => Assert.False(string.IsNullOrWhiteSpace(row.Name)));
    }

    [Fact]
    public void Get_eventlog_has_name_display_and_status()
    {
        var row = ServiceHelper.Get("eventlog", ServiceDetailLevel.Slim, joinProcess: false);
        Assert.NotNull(row);
        Assert.Equal("EventLog", row!.Name, ignoreCase: true);
        Assert.False(string.IsNullOrWhiteSpace(row.DisplayName));
        Assert.NotEqual(ServiceStatus.Unknown, row.Status);
    }

    [Fact]
    public void Get_missing_name_returns_null()
        => Assert.Null(ServiceHelper.Get("NoSuchService_Vestigium", ServiceDetailLevel.Identity, joinProcess: false));

    [Fact]
    public void TryGet_blank_is_false()
        => Assert.False(ServiceHelper.TryGet("   ", out _));

    [Fact]
    public void Search_contains_spooler()
    {
        var hits = ServiceHelper.Search("spool", ServiceSearchMode.Contains);
        Assert.Contains(
            hits,
            row => row.Name.Contains("spool", StringComparison.OrdinalIgnoreCase)
                || (row.DisplayName?.Contains("spool", StringComparison.OrdinalIgnoreCase) ?? false));
    }

    [Fact]
    public void Search_starts_with_event()
        => Assert.Contains(
            ServiceHelper.Search("Event", ServiceSearchMode.StartsWith, ServiceSearchFields.Name),
            row => row.Name.StartsWith("Event", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void Search_ends_with_log()
        => Assert.Contains(
            ServiceHelper.Search("Log", ServiceSearchMode.EndsWith, ServiceSearchFields.Name),
            row => row.Name.EndsWith("Log", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void Search_blank_throws()
        => Assert.Throws<ArgumentException>(() => ServiceHelper.Search("  ", ServiceSearchMode.Contains));

    [Fact]
    public void Search_over_cap_throws()
        => Assert.Throws<ArgumentException>(() => ServiceHelper.Search("e", ServiceSearchMode.Contains, maxResults: 300));
}
