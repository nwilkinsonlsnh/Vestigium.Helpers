using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ServicePhase5Tests
{
    [Fact]
    public void SetLogon_without_confirm_is_denied()
    {
        var result = ServiceHelper.SetLogon("Spooler", new ServiceLogonRequest { Confirm = false });
        Assert.Equal(ServiceControlStatus.Denied, result.Status);
        Assert.Contains("confirm", result.Reason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetLogon_eventlog_is_protected()
    {
        var result = ServiceHelper.SetLogon("EventLog", new ServiceLogonRequest
        {
            Kind = ServiceLogonKind.LocalSystem,
            Confirm = true
        });
        Assert.Equal(ServiceControlStatus.Denied, result.Status);
        Assert.Contains("protected", result.Reason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Interact_with_desktop_requires_local_system()
    {
        var result = ServiceHelper.SetLogon("Spooler", new ServiceLogonRequest
        {
            Kind = ServiceLogonKind.Account,
            Account = @"NT AUTHORITY\LocalService",
            InteractWithDesktop = true,
            Confirm = true
        });
        Assert.Equal(ServiceControlStatus.InvalidState, result.Status);
    }

    [Fact]
    public void Account_kind_requires_account_name()
        => Assert.Throws<ArgumentException>(() => ServiceHelper.SetLogon("Spooler", new ServiceLogonRequest
        {
            Kind = ServiceLogonKind.Account,
            Confirm = true
        }));

    [Fact]
    public void Query_logon_rights_does_not_throw()
    {
        var info = ServiceHelper.QueryLogonRights("NT AUTHORITY\\SYSTEM");
        Assert.Equal("NT AUTHORITY\\SYSTEM", info.Account, ignoreCase: true);
    }

    [Fact]
    public void Grant_none_is_query_only()
    {
        var info = ServiceHelper.GrantLogonRights("NT AUTHORITY\\SYSTEM", ServiceGrantLogonRight.None);
        Assert.NotNull(info.Account);
    }

    [Fact]
    public void Get_eventlog_account_never_exposes_password()
    {
        var row = ServiceHelper.Get("EventLog", ServiceDetailLevel.Slim, joinProcess: false);
        Assert.NotNull(row);
        Assert.False(string.IsNullOrWhiteSpace(row!.Account));
        Assert.DoesNotContain(
            typeof(ServiceInfo).GetProperties(),
            p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            typeof(ServiceControlResult).GetProperties(),
            p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SetLogon_null_request_throws()
        => Assert.Throws<ArgumentNullException>(() => ServiceHelper.SetLogon("Spooler", null!));
}
