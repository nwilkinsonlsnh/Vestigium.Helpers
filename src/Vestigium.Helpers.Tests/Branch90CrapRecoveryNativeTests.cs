using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90CrapRecoveryNativeTests
{
    [Fact]
    public void Set_opens_real_service_and_restores()
    {
        Assert.Equal(ServiceControlStatus.InvalidState, ServiceRecovery.Set("Spooler", new ServiceRecoveryRequest
        {
            Confirm = true,
            FirstFailure = ServiceFailureActionKind.RunCommand
        }).Status);

        var target = new[] { "Fax", "PrintNotify", "MapsBroker", "Themes", "Spooler" }
            .FirstOrDefault(name => !ServiceControl.IsProtected(name) && ServiceHelper.Get(name) is not null);
        if (target is null)
            return;

        var before = ServiceRecovery.Get(target);
        try
        {
            var set = ServiceRecovery.Set(target, new ServiceRecoveryRequest
            {
                Confirm = true,
                FirstFailure = ServiceFailureActionKind.None,
                SecondFailure = ServiceFailureActionKind.Restart,
                SubsequentFailures = ServiceFailureActionKind.None,
                ActionDelay = TimeSpan.FromSeconds(1),
                ResetPeriod = TimeSpan.FromDays(1),
                RebootMessage = "vest-crap",
                Command = "cmd.exe"
            });
            Assert.True(set.Status is ServiceControlStatus.Ok or ServiceControlStatus.Denied, set.Reason);
        }
        finally
        {
            if (before is not null)
            {
                _ = ServiceRecovery.Set(target, new ServiceRecoveryRequest
                {
                    Confirm = true,
                    FirstFailure = before.FirstFailure ?? ServiceFailureActionKind.None,
                    SecondFailure = before.SecondFailure ?? ServiceFailureActionKind.None,
                    SubsequentFailures = before.SubsequentFailures ?? ServiceFailureActionKind.None,
                    ActionDelay = before.Actions.Count > 0 ? before.Actions[0].Delay : TimeSpan.FromMinutes(1),
                    ResetPeriod = before.ResetPeriod ?? TimeSpan.FromDays(1),
                    Command = before.Command,
                    RebootMessage = before.RebootMessage
                });
            }
        }
    }
}
