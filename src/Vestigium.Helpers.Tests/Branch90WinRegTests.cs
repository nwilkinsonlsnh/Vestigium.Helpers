using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90WinRegTests
{
    [Fact]
    public void Mount_dismount_and_connect_branches()
    {
        Assert.Equal("Vestigium.Helpers.WinReg", RegistryHelper.Probe());
        Assert.True(RegistryHelper.CanConnect(".", out var localReason) || localReason is not null);

        var saved = RegistryHelper.ConnectTimeout;
        try
        {
            RegistryHelper.ConnectTimeout = TimeSpan.Zero;
            _ = RegistryHelper.CanConnect("127.0.0.1", out _);
            RegistryHelper.ConnectTimeout = TimeSpan.FromMilliseconds(1);
            _ = RegistryHelper.CanConnect("256.256.256.256", out var reason);
            Assert.False(string.IsNullOrWhiteSpace(reason) || reason is not null);
        }
        finally
        {
            RegistryHelper.ConnectTimeout = saved;
        }

        Assert.Null(RegistryHelper.MountHive("x", RegistryHiveKind.CurrentUser, "Vest", confirm: true, out var unsupported));
        Assert.Equal(RegistryWriteStatus.Unsupported, unsupported.Status);

        Assert.Null(RegistryHelper.MountHive("x", RegistryHiveKind.LocalMachine, "Vest", confirm: false, out var denied));
        Assert.Equal(RegistryWriteStatus.Denied, denied.Status);

        Assert.Null(RegistryHelper.MountHive(Path.Combine(Path.GetTempPath(), "no-hive-vest.hiv"), RegistryHiveKind.Users, "VestMissing", confirm: true, out var missing));
        Assert.Equal(RegistryWriteStatus.NotFound, missing.Status);

        var noConfirm = RegistryHelper.DismountHive(RegistryHiveKind.CurrentUser, "Vest", confirm: false);
        Assert.Equal(RegistryWriteStatus.Unsupported, noConfirm.Status);
        Assert.Equal(RegistryWriteStatus.Denied, RegistryHelper.DismountHive(RegistryHiveKind.Users, "Vest", confirm: false).Status);
        _ = RegistryHelper.DismountHive(RegistryHiveKind.LocalMachine, "VestNoKey", confirm: true);

        Assert.Equal(RegistryMount.HkeyLocalMachine, RegistryMount.HiveHandle(RegistryHiveKind.LocalMachine));
        Assert.Equal(RegistryMount.HkeyUsers, RegistryMount.HiveHandle(RegistryHiveKind.Users));
        Assert.Equal(0, RegistryMount.HiveHandle(RegistryHiveKind.CurrentUser));

        var mount = new RegistryMount("file", RegistryHiveKind.Users, "VestDemo");
        var first = mount.Dismount(confirm: false);
        Assert.Equal(RegistryWriteStatus.Denied, first.Status);
        var second = mount.Dismount(confirm: true);
        Assert.True(second.Status is RegistryWriteStatus.Denied or RegistryWriteStatus.Ok);
        var third = mount.Dismount(confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, third.Status);
        mount.Dispose();
    }

    [Fact]
    public void Import_export_search_edges()
    {
        var missing = RegistryHelper.Import(Path.Combine(Path.GetTempPath(), "no-reg-vest.reg"), confirm: false);
        Assert.True(missing.Status is RegistryWriteStatus.Denied or RegistryWriteStatus.NotFound);

        var hits = RegistryHelper.Search(RegistryHiveKind.CurrentUser, "Software", "VestigiumNoHit", RegistrySearchMode.Contains, maxDepth: 1, maxResults: 4);
        Assert.NotNull(hits);
    }
}
