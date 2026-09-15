using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90CrapLogonTests
{
    [Fact]
    public void Logon_grant_starttype_open_and_share_process()
    {
        var missing = ServiceLogon.Set("NoSuchService_Vestigium", new ServiceLogonRequest
        {
            Confirm = true,
            Kind = ServiceLogonKind.NetworkService
        });
        Assert.True(missing.Status is ServiceControlStatus.Denied or ServiceControlStatus.NotFound);

        var account = ServiceLogon.Set("NoSuchService_Vestigium", new ServiceLogonRequest
        {
            Confirm = true,
            Kind = ServiceLogonKind.Account,
            Account = Environment.UserName,
            Password = "x",
            GrantLogonRight = ServiceGrantLogonRight.ServiceAndBatch
        });
        Assert.True(account.Status is ServiceControlStatus.Denied or ServiceControlStatus.NotFound or ServiceControlStatus.InvalidState);

        var share = ServiceHelper.List(ServiceDetailLevel.Slim, ServiceKind.Win32)
            .FirstOrDefault(s => s.SharedProcess && !ServiceControl.IsProtected(s.Name));
        if (share is not null)
        {
            var interact = ServiceLogon.Set(share.Name, new ServiceLogonRequest
            {
                Confirm = true,
                Kind = ServiceLogonKind.LocalSystem,
                InteractWithDesktop = true
            });
            Assert.True(interact.Status is ServiceControlStatus.InvalidState or ServiceControlStatus.Denied or ServiceControlStatus.Ok);
        }

        _ = ServiceLogon.Grant(Environment.UserName, ServiceGrantLogonRight.ServiceAndBatch);
        _ = ServiceLogon.QueryRights(Environment.UserName);

        Assert.Equal(ServiceControlStatus.Denied, ServiceControl.SetStartType("NoSuchService_Vestigium", ServiceStartType.Manual, confirm: true).Status);
        Assert.Equal(ServiceControlStatus.Denied, ServiceControl.SetStartType("EventLog", ServiceStartType.AutomaticDelayed, confirm: true).Status);
        Assert.Equal(ServiceControlStatus.Unsupported, ServiceControl.SetStartType("Spooler", ServiceStartType.System, confirm: true).Status);

        var recovery = ServiceRecovery.Set("NoSuchService_Vestigium", new ServiceRecoveryRequest
        {
            Confirm = true,
            FirstFailure = ServiceFailureActionKind.Restart,
            SecondFailure = ServiceFailureActionKind.Reboot,
            SubsequentFailures = ServiceFailureActionKind.RunCommand,
            Command = "cmd.exe",
            RebootMessage = "reboot",
            ResetPeriod = TimeSpan.FromDays(1),
            ActionDelay = TimeSpan.FromMinutes(1)
        });
        Assert.Equal(ServiceControlStatus.NotFound, recovery.Status);
    }
}
