using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90Wave2ServicesTests
{
    [Fact]
    public void Logon_and_recovery_early_outs()
    {
        Assert.Throws<ArgumentNullException>(() => ServiceLogon.Set("x", null!));
        Assert.Equal(ServiceControlStatus.Denied, ServiceLogon.Set("Spooler", new ServiceLogonRequest { Confirm = false }).Status);
        Assert.Equal(ServiceControlStatus.Denied, ServiceLogon.Set("EventLog", new ServiceLogonRequest { Confirm = true }).Status);
        Assert.Equal(ServiceControlStatus.InvalidState, ServiceLogon.Set("Spooler", new ServiceLogonRequest
        {
            Confirm = true,
            Kind = ServiceLogonKind.LocalService,
            InteractWithDesktop = true
        }).Status);
        Assert.Throws<ArgumentException>(() => ServiceLogon.Set("Spooler", new ServiceLogonRequest
        {
            Confirm = true,
            Kind = ServiceLogonKind.Account,
            Account = "  "
        }));

        var rights = ServiceLogon.QueryRights("LocalSystem");
        Assert.False(string.IsNullOrWhiteSpace(rights.Account));
        _ = ServiceLogon.QueryRights("SYSTEM");
        _ = ServiceLogon.QueryRights(@".\LocalSystem");
        _ = ServiceLogon.QueryRights("NoSuchAccount_Vestigium_ZZZ");
        _ = ServiceLogon.Grant("NT AUTHORITY\\SYSTEM", ServiceGrantLogonRight.None);
        _ = ServiceLogon.Grant("NoSuchAccount_Vestigium_ZZZ", ServiceGrantLogonRight.Service);
        _ = ServiceLogon.Grant("NoSuchAccount_Vestigium_ZZZ", ServiceGrantLogonRight.Batch);

        var missingLogon = ServiceLogon.Set("NoSuchService_Vestigium", new ServiceLogonRequest
        {
            Confirm = true,
            Kind = ServiceLogonKind.LocalService
        });
        Assert.True(missingLogon.Status is ServiceControlStatus.Denied or ServiceControlStatus.NotFound);

        Assert.Throws<ArgumentNullException>(() => ServiceRecovery.Set("x", null!));
        Assert.Equal(ServiceControlStatus.Denied, ServiceRecovery.Set("Spooler", new ServiceRecoveryRequest { Confirm = false }).Status);
        Assert.Equal(ServiceControlStatus.Denied, ServiceRecovery.Set("EventLog", new ServiceRecoveryRequest { Confirm = true }).Status);
        Assert.Equal(ServiceControlStatus.NotFound, ServiceRecovery.Set("NoSuchService_Vestigium", new ServiceRecoveryRequest { Confirm = true }).Status);
        Assert.Equal(ServiceControlStatus.InvalidState, ServiceRecovery.Set("Spooler", new ServiceRecoveryRequest
        {
            Confirm = true,
            ResetPeriod = TimeSpan.FromSeconds(-1)
        }).Status);
        Assert.Equal(ServiceControlStatus.InvalidState, ServiceRecovery.Set("Spooler", new ServiceRecoveryRequest
        {
            Confirm = true,
            ActionDelay = TimeSpan.FromSeconds(-1)
        }).Status);
        Assert.Equal(ServiceControlStatus.InvalidState, ServiceRecovery.Set("Spooler", new ServiceRecoveryRequest
        {
            Confirm = true,
            FirstFailure = (ServiceFailureActionKind)99
        }).Status);
        Assert.Equal(ServiceControlStatus.InvalidState, ServiceRecovery.Set("Spooler", new ServiceRecoveryRequest
        {
            Confirm = true,
            FirstFailure = ServiceFailureActionKind.RunCommand,
            Command = "  "
        }).Status);
        Assert.Null(ServiceRecovery.Get("NoSuchService_Vestigium"));
        _ = ServiceRecovery.Get("EventLog");
    }

    [Fact]
    public void Client_local_hits_every_facade()
    {
        var client = ServiceHelper.Local;
        Assert.True(client.IsLocal);
        Assert.Null(client.Machine);
        Assert.True(client.CanConnect(out _));
        Assert.NotEmpty(client.List());
        _ = client.ListHidden();
        Assert.NotNull(client.Get("EventLog"));
        Assert.True(client.TryGet("EventLog", out _));
        Assert.False(client.TryGet("NoSuchService_Vestigium", out _));
        _ = client.Search("Event", ServiceSearchMode.Contains);
        _ = client.Search("SVC.Name == 'EventLog'");
        _ = client.GetDependencyTree("EventLog");
        _ = client.Start("NoSuchService_Vestigium");
        _ = client.Stop("EventLog");
        _ = client.Restart("EventLog");
        _ = client.Pause("EventLog");
        _ = client.Continue("EventLog");
        _ = client.SetStartType("EventLog", ServiceStartType.Automatic, confirm: false);
        _ = client.SetLogon("EventLog", new ServiceLogonRequest { Confirm = false });
        _ = client.SetRecovery("EventLog", new ServiceRecoveryRequest { Confirm = false });
        _ = client.GetRecovery("EventLog");
        using var watch = client.Watch("EventLog", TimeSpan.FromMilliseconds(250));
        Assert.Equal(TimeSpan.FromMilliseconds(250), watch.Interval);

        var remote = ServiceHelper.For(Environment.MachineName);
        Assert.False(string.IsNullOrWhiteSpace(remote.Machine) && remote.IsLocal);
        _ = remote.CanConnect(out _);
        Assert.Throws<ArgumentException>(() => ServiceHelper.For("  "));
        _ = ServiceHelper.CanConnect(Environment.MachineName);
    }
}
