using Vestigium.Helpers.Processes;
using Vestigium.Helpers.Services;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90FullSnapshotTests
{
    [Fact]
    public void Process_and_service_full_levels()
    {
        var me = ProcessHelper.Get(Environment.ProcessId, ProcessDetailLevel.Full);
        Assert.NotNull(me);
        _ = ProcessHelper.Get(Environment.ProcessId, ProcessDetailLevel.Identity);
        _ = ProcessHelper.Get(Environment.ProcessId, ProcessDetailLevel.Slim);
        _ = ProcessHelper.List(ProcessDetailLevel.Identity);
        _ = ProcessHelper.List(ProcessDetailLevel.Slim);
        _ = ProcessHelper.List(ProcessDetailLevel.Full);
        _ = ProcessHelper.GetThreads(Environment.ProcessId, includeStack: true);
        _ = ProcessHelper.Search("PID == " + Environment.ProcessId, ProcessDetailLevel.Full);
        if (!string.IsNullOrWhiteSpace(me!.Name))
            _ = ProcessHelper.Search(me.Name, ProcessSearchMode.Contains, ProcessSearchFields.Name, ProcessDetailLevel.Slim);
        _ = ProcessHelper.GetTree(Environment.ProcessId);

        _ = ServiceHelper.List(ServiceDetailLevel.Identity, ServiceKind.All, ServiceListScope.All);
        _ = ServiceHelper.List(ServiceDetailLevel.Slim, ServiceKind.Win32, ServiceListScope.Visible, joinProcess: true);
        _ = ServiceHelper.List(ServiceDetailLevel.Full, ServiceKind.Driver, ServiceListScope.Hidden);
        _ = ServiceHelper.Get("EventLog", ServiceDetailLevel.Full, joinProcess: true);
        _ = ServiceHelper.Get("EventLog", ServiceDetailLevel.Identity, joinProcess: false);
        _ = ServiceHelper.ListHidden(ServiceDetailLevel.Full, ServiceKind.All);
        _ = ServiceHelper.GetDependsOn("EventLog");
        _ = ServiceHelper.GetDependedBy("EventLog");
        _ = ServiceHelper.GetDependencyTree("EventLog", ServiceTreeDirection.Both);
        _ = ServiceHelper.GetDependencyTree("EventLog", ServiceTreeDirection.DependedBy);
    }

    [Fact]
    public void Edit_list_round_trip_and_apply_unknown()
    {
        var hive = RegistryHiveKind.CurrentUser;
        var root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        var dir = Path.Combine(Path.GetTempPath(), "vest-edits-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var list = new RegistryEditList { Label = "walk" }
                .CreateKey(hive, root)
                .SetValue(hive, root, "A", "1")
                .SetValue(hive, root, "D", 4, RegistryValueKind.DWord)
                .RenameValue(hive, root, "A", "B")
                .DeleteValue(hive, root, "D")
                .CreateKey(hive, root + "\\Z")
                .RenameKey(hive, root + "\\Z", root + "\\Y")
                .DeleteKey(hive, root + "\\Y", recursive: true);
            var edits = Path.Combine(dir, "edits.json");
            list.Save(edits);
            var loaded = RegistryEditList.Load(edits);
            Assert.NotEmpty(loaded.Ops);
            var journal = Path.Combine(dir, "edits.jnl");
            var applied = RegistryHelper.Apply(edits, journal, confirm: true);
            Assert.True(applied.Status is RegistryWriteStatus.Ok or RegistryWriteStatus.Denied or RegistryWriteStatus.NotFound);

            var unknown = new RegistryEditList { Label = "bad" };
            unknown.Ops.Add(new RegistryEditOp { Op = "Nope", Hive = hive, Path = root });
            var bad = RegistryHelper.Apply(unknown, Path.Combine(dir, "bad.jnl"), confirm: true);
            Assert.Equal(RegistryWriteStatus.InvalidPath, bad.Status);
            RegistryHelper.Local.DeleteKey(hive, root, recursive: true, confirm: true);
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }
}
