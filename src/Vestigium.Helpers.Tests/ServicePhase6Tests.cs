using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ServicePhase6Tests
{
    [Fact]
    public void GetRecovery_eventlog_has_actions_list()
    {
        var info = ServiceHelper.GetRecovery("EventLog");
        Assert.NotNull(info);
        Assert.Equal("EventLog", info!.Name, ignoreCase: true);
        Assert.NotNull(info.Actions);
    }

    [Fact]
    public void GetRecovery_missing_is_null()
        => Assert.Null(ServiceHelper.GetRecovery("NoSuchService_Vestigium"));

    [Fact]
    public void SetRecovery_without_confirm_is_denied()
    {
        var result = ServiceHelper.SetRecovery("Spooler", new ServiceRecoveryRequest { Confirm = false });
        Assert.Equal(ServiceControlStatus.Denied, result.Status);
        Assert.Contains("confirm", result.Reason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetRecovery_eventlog_is_protected()
    {
        var result = ServiceHelper.SetRecovery("EventLog", new ServiceRecoveryRequest
        {
            FirstFailure = ServiceFailureActionKind.Restart,
            SecondFailure = ServiceFailureActionKind.Restart,
            SubsequentFailures = ServiceFailureActionKind.None,
            ResetPeriod = TimeSpan.FromDays(1),
            Confirm = true
        });
        Assert.Equal(ServiceControlStatus.Denied, result.Status);
        Assert.Contains("protected", result.Reason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetRecovery_missing_is_not_found()
    {
        var result = ServiceHelper.SetRecovery("NoSuchService_Vestigium", new ServiceRecoveryRequest { Confirm = true });
        Assert.Equal(ServiceControlStatus.NotFound, result.Status);
    }

    [Fact]
    public void SetRecovery_run_command_without_command_is_invalid()
    {
        var result = ServiceHelper.SetRecovery("Spooler", new ServiceRecoveryRequest
        {
            FirstFailure = ServiceFailureActionKind.RunCommand,
            Confirm = true
        });
        Assert.Equal(ServiceControlStatus.InvalidState, result.Status);
    }

    [Fact]
    public void SetRecovery_negative_reset_is_invalid()
    {
        var result = ServiceHelper.SetRecovery("Spooler", new ServiceRecoveryRequest
        {
            ResetPeriod = TimeSpan.FromDays(-1),
            Confirm = true
        });
        Assert.Equal(ServiceControlStatus.InvalidState, result.Status);
    }

    [Fact]
    public void SetRecovery_null_request_throws()
        => Assert.Throws<ArgumentNullException>(() => ServiceHelper.SetRecovery("Spooler", null!));
}
