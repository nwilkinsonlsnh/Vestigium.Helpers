using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryPhase6Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryPhase6Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _dir = Path.Combine(Path.GetTempPath(), "vest-winreg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.CreateKey(Hive, _root, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "Mark", "mounted", confirm: true).Status);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Mount_without_confirm_does_not_load()
    {
        var file = Path.Combine(_dir, "x.hiv");
        File.WriteAllBytes(file, [1, 2, 3]);
        var mount = RegistryHelper.MountHive(file, RegistryHiveKind.LocalMachine, "VESTIGIUM_TEST_NO", confirm: false, out var result);
        Assert.Null(mount);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
    }

    [Fact]
    public void Mount_missing_file_is_not_found()
    {
        var mount = RegistryHelper.MountHive(Path.Combine(_dir, "gone.hiv"), RegistryHiveKind.LocalMachine, "VESTIGIUM_TEST_MISS", confirm: true, out var result);
        Assert.Null(mount);
        Assert.Equal(RegistryWriteStatus.NotFound, result.Status);
    }

    [Fact]
    public void Mount_current_user_is_unsupported()
    {
        var file = Path.Combine(_dir, "x.hiv");
        File.WriteAllBytes(file, [1]);
        var mount = RegistryHelper.MountHive(file, RegistryHiveKind.CurrentUser, "VESTIGIUM_TEST_CU", confirm: true, out var result);
        Assert.Null(mount);
        Assert.Equal(RegistryWriteStatus.Unsupported, result.Status);
    }

    [Fact]
    public void Mount_exported_hive_ok_or_denied()
    {
        var file = Path.Combine(_dir, "out.hiv");
        var exported = RegistryHelper.Export(file, Hive, _root, RegistryExportFormat.HiveFile, confirm: true);
        if (exported.Status != RegistryWriteStatus.Ok)
            return;

        var sub = "VESTIGIUM_TEST_" + Guid.NewGuid().ToString("N")[..8];
        var mount = RegistryHelper.MountHive(file, RegistryHiveKind.LocalMachine, sub, confirm: true, out var result);
        Assert.True(result.Status is RegistryWriteStatus.Ok or RegistryWriteStatus.Denied);
        if (mount is null)
            return;

        try
        {
            Assert.True(mount.IsLoaded);
            var value = mount.Client.GetValue(RegistryHiveKind.LocalMachine, sub, "Mark");
            Assert.True(value is null || Equals(value.Data, "mounted"));
            var second = mount.Dismount(confirm: true);
            Assert.Equal(RegistryWriteStatus.Ok, second.Status);
            Assert.Equal(RegistryWriteStatus.Ok, mount.Dismount(confirm: true).Status);
        }
        finally
        {
            mount.Dispose();
        }
    }
}
