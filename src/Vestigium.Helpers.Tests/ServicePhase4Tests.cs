using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ServicePhase4Tests
{
    [Fact]
    public void Protected_names_include_eventlog()
        => Assert.Contains("EventLog", ServiceHelper.ProtectedNames, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Start_eventlog_is_already_running_or_denied_not_thrown()
    {
        var result = ServiceHelper.Start("EventLog");
        Assert.True(
            result.Status is ServiceControlStatus.Ok or ServiceControlStatus.Denied or ServiceControlStatus.InvalidState,
            result.Reason);
        if (result.Status == ServiceControlStatus.Ok)
            Assert.Equal(ServiceStatus.Running, result.ResultingState);
    }

    [Fact]
    public void Stop_eventlog_is_denied_protected()
    {
        var result = ServiceHelper.Stop("EventLog", confirmDependents: true);
        Assert.Equal(ServiceControlStatus.Denied, result.Status);
        Assert.Contains("protected", result.Reason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Restart_eventlog_is_denied_protected()
        => Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.Restart("EventLog", confirmDependents: true).Status);

    [Fact]
    public void SetStartType_without_confirm_is_denied()
        => Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.SetStartType("EventLog", ServiceStartType.Automatic, confirm: false).Status);

    [Fact]
    public void SetStartType_protected_with_confirm_is_still_denied()
        => Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.SetStartType("EventLog", ServiceStartType.Automatic, confirm: true).Status);

    [Fact]
    public void SetStartType_boot_is_unsupported()
        => Assert.Equal(ServiceControlStatus.Unsupported, ServiceHelper.SetStartType("Spooler", ServiceStartType.Boot, confirm: true).Status);

    [Fact]
    public void Pause_eventlog_is_unsupported_or_denied()
    {
        var result = ServiceHelper.Pause("EventLog");
        Assert.True(
            result.Status is ServiceControlStatus.Unsupported or ServiceControlStatus.Denied or ServiceControlStatus.InvalidState,
            result.Reason);
    }

    [Fact]
    public void Continue_eventlog_is_unsupported_or_denied()
    {
        var result = ServiceHelper.Continue("EventLog");
        Assert.True(
            result.Status is ServiceControlStatus.Unsupported or ServiceControlStatus.Denied or ServiceControlStatus.InvalidState or ServiceControlStatus.Ok,
            result.Reason);
    }

    [Fact]
    public void Missing_service_is_not_found()
    {
        Assert.Equal(ServiceControlStatus.NotFound, ServiceHelper.Start("NoSuchService_Vestigium").Status);
        Assert.Equal(ServiceControlStatus.NotFound, ServiceHelper.Stop("NoSuchService_Vestigium").Status);
        Assert.Equal(ServiceControlStatus.NotFound, ServiceHelper.Pause("NoSuchService_Vestigium").Status);
    }

    [Fact]
    public void Control_never_throws_on_access()
    {
        var ex = Record.Exception(() =>
        {
            ServiceHelper.Start("EventLog");
            ServiceHelper.Stop("EventLog");
            ServiceHelper.Pause("EventLog");
            ServiceHelper.Continue("EventLog");
            ServiceHelper.Restart("EventLog");
            ServiceHelper.SetStartType("EventLog", ServiceStartType.Manual, confirm: false);
        });
        Assert.Null(ex);
    }
}
