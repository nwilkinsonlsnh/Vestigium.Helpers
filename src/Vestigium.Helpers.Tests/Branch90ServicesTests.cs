using System.Diagnostics;
using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Processes;
using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90ServicesTests
{
    [Fact]
    public void Kql_row_covers_filled_and_empty()
    {
        var filled = new ServiceInfo
        {
            Name = "svc",
            DisplayName = "Display",
            Status = ServiceStatus.Running,
            StartType = ServiceStartType.Automatic,
            Pid = 9,
            ImagePath = @"C:\Windows\System32\svchost.exe",
            Account = @"NT AUTHORITY\SYSTEM",
            Kind = ServiceKind.Win32,
            SharedProcess = true,
            IsHidden = false,
            DelayedAutoStart = true,
            TriggerStart = false,
            LaunchProtected = ServiceLaunchProtected.Windows,
            DependsOn = ["RpcSs"]
        };
        var row = new ServiceKqlRow(filled);
        foreach (var name in new[]
                 {
                     "SVC.Name", "SVC.DisplayName", "SVC.Status", "SVC.StartType", "SVC.Pid", "SVC.ImagePath",
                     "SVC.Account", "SVC.Kind", "SVC.SharedProcess", "SVC.Hidden", "SVC.DelayedAutoStart",
                     "SVC.TriggerStart", "SVC.LaunchProtected", "SVC.DependsOn"
                 })
            Assert.False(row.Get(name).IsUnknown, name);
        Assert.True(row.Get("nope").IsUnknown);

        var empty = new ServiceKqlRow(new ServiceInfo { Name = "x", Status = ServiceStatus.Stopped });
        Assert.False(empty.Get("SVC.Name").IsUnknown);
        Assert.True(empty.Get("SVC.DisplayName").IsUnknown);
        Assert.True(empty.Get("SVC.StartType").IsUnknown);
        Assert.True(empty.Get("SVC.Pid").IsUnknown);
        Assert.True(empty.Get("SVC.ImagePath").IsUnknown);
        Assert.True(empty.Get("SVC.Account").IsUnknown);
        Assert.True(empty.Get("SVC.DelayedAutoStart").IsUnknown);
        Assert.True(empty.Get("SVC.TriggerStart").IsUnknown);
        Assert.True(empty.Get("SVC.LaunchProtected").IsUnknown);
        Assert.True(empty.Get("SVC.DependsOn").IsUnknown);
    }

    [Fact]
    public void Search_modes_and_caps_and_kql()
    {
        Assert.Equal("Vestigium.Helpers.Services", ServiceHelper.Probe());
        Assert.False(ServiceHelper.TryGet("  ", out _));
        Assert.False(ServiceHelper.TryGet("NoSuchService_Vestigium", out _));
        Assert.Null(ServiceHelper.Get("NoSuchService_Vestigium"));

        var eventLog = ServiceHelper.Search("EventLog", ServiceSearchMode.Contains, ServiceSearchFields.Name);
        Assert.Contains(eventLog, r => string.Equals(r.Name, "EventLog", StringComparison.OrdinalIgnoreCase));

        _ = ServiceHelper.Search("Event", ServiceSearchMode.StartsWith, ServiceSearchFields.Name);
        _ = ServiceHelper.Search("Log", ServiceSearchMode.EndsWith, ServiceSearchFields.DisplayName);
        _ = ServiceHelper.Search("event", ServiceSearchMode.Contains, ServiceSearchFields.Default);
        _ = ServiceHelper.Search("x", ServiceSearchMode.Contains, ServiceSearchFields.Description);
        _ = ServiceHelper.Search("x", ServiceSearchMode.Contains, ServiceSearchFields.ImagePath);
        _ = ServiceHelper.Search("x", ServiceSearchMode.Contains, ServiceSearchFields.Account);
        _ = ServiceHelper.Search("EventLog", ServiceSearchMode.Contains, 0);
        _ = ServiceHelper.Search("SVC.Name == 'EventLog'");

        Assert.Throws<ArgumentException>(() => ServiceHelper.Search("  ", ServiceSearchMode.Contains));
        Assert.Throws<ArgumentOutOfRangeException>(() => ServiceHelper.Search("a", ServiceSearchMode.Contains, maxResults: 0));
        Assert.Throws<ArgumentException>(() => ServiceHelper.Search("a", ServiceSearchMode.Contains, maxResults: 300));
        Assert.Throws<ArgumentException>(() => ServiceHelper.Search("Nope == 1"));
        Assert.Throws<ArgumentException>(() => ServiceHelper.Search("  "));
        Assert.Empty(ServiceHelper.GetDependsOn("NoSuchService_Vestigium"));
        Assert.Empty(ServiceHelper.GetDependedBy("NoSuchService_Vestigium"));
    }

    [Fact]
    public void Control_logon_recovery_watch_deny_paths()
    {
        var missing = "NoSuchService_Vestigium";
        _ = ServiceHelper.Start(missing);
        _ = ServiceHelper.Stop(missing);
        _ = ServiceHelper.Pause(missing);
        _ = ServiceHelper.Continue(missing);
        _ = ServiceHelper.Restart(missing);
        _ = ServiceHelper.SetStartType(missing, ServiceStartType.Manual, confirm: false);
        _ = ServiceHelper.SetStartType("EventLog", ServiceStartType.Automatic, confirm: false);
        _ = ServiceHelper.SetLogon("EventLog", new ServiceLogonRequest { Confirm = false });
        _ = ServiceHelper.SetRecovery("EventLog", new ServiceRecoveryRequest { Confirm = false });
        _ = ServiceHelper.GetRecovery("EventLog");
        _ = ServiceHelper.GetRecovery(missing);
        _ = ServiceHelper.QueryLogonRights("NT AUTHORITY\\SYSTEM");
        _ = ServiceHelper.GrantLogonRights("NT AUTHORITY\\SYSTEM", ServiceGrantLogonRight.None);

        Assert.Contains(ServiceHelper.ProtectedNames, n => n.Contains("EventLog", StringComparison.OrdinalIgnoreCase) || n.Length > 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => ServiceHelper.Watch("EventLog", TimeSpan.FromMilliseconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => ServiceHelper.WatchQuery("SVC.Name == 'EventLog'", TimeSpan.FromHours(2)));
        Assert.Throws<ArgumentException>(() => ServiceHelper.WatchQuery("Nope == 1", TimeSpan.FromSeconds(1)));

        using (var watch = ServiceHelper.Watch("EventLog", TimeSpan.FromMilliseconds(250)))
            Assert.Equal(TimeSpan.FromMilliseconds(250), watch.Interval);
        using (var q = ServiceHelper.WatchQuery("SVC.Name == 'EventLog'", TimeSpan.FromMilliseconds(250)))
            Assert.NotNull(q);

        var info = new ServiceRecoveryInfo { Name = "x", Actions = [] };
        Assert.Null(info.FirstFailure);
        info = new ServiceRecoveryInfo { Name = "x", Actions = [new ServiceFailureAction(ServiceFailureActionKind.Restart, TimeSpan.Zero)] };
        Assert.Equal(ServiceFailureActionKind.Restart, info.FirstFailure);
        Assert.Null(info.SecondFailure);
        info = new ServiceRecoveryInfo
        {
            Name = "x",
            Actions =
            [
                new ServiceFailureAction(ServiceFailureActionKind.Restart, TimeSpan.Zero),
                new ServiceFailureAction(ServiceFailureActionKind.Reboot, TimeSpan.Zero),
                new ServiceFailureAction(ServiceFailureActionKind.RunCommand, TimeSpan.Zero)
            ]
        };
        Assert.Equal(ServiceFailureActionKind.RunCommand, info.SubsequentFailures);
    }

    [Fact]
    public void Process_access_gone_and_denied()
    {
        using var live = Process.GetCurrentProcess();
        Assert.Equal(Availability.Denied, ProcessAccess.Of(live));

        var ping = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "ping.exe"),
            Arguments = "-n 1 127.0.0.1",
            CreateNoWindow = true,
            UseShellExecute = false
        });
        Assert.NotNull(ping);
        ping!.Kill();
        ping.WaitForExit(5000);
        Assert.Equal(Availability.Gone, ProcessAccess.Of(ping));
        ping.Dispose();
    }
}
