using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90WinRegTests
{
    [Fact]
    public void Mount_dismount_and_connect_branches()
    {
        Assert.Equal("Vestigium.Helpers.WinReg", RegistryHelper.Probe());
        _ = RegistryHelper.CanConnect(".", out _);

        var saved = RegistryHelper.ConnectTimeout;
        try
        {
            RegistryHelper.ConnectTimeout = TimeSpan.Zero;
            _ = RegistryHelper.CanConnect("127.0.0.1", out _);
            RegistryHelper.ConnectTimeout = TimeSpan.FromMilliseconds(1);
            _ = RegistryHelper.CanConnect("256.256.256.256", out _);
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

        Assert.Equal(RegistryWriteStatus.Unsupported, RegistryHelper.DismountHive(RegistryHiveKind.CurrentUser, "Vest", confirm: false).Status);
        Assert.Equal(RegistryWriteStatus.Denied, RegistryHelper.DismountHive(RegistryHiveKind.Users, "Vest", confirm: false).Status);
        _ = RegistryHelper.DismountHive(RegistryHiveKind.LocalMachine, "VestNoKey", confirm: true);

        Assert.Equal(RegistryMount.HkeyLocalMachine, RegistryMount.HiveHandle(RegistryHiveKind.LocalMachine));
        Assert.Equal(RegistryMount.HkeyUsers, RegistryMount.HiveHandle(RegistryHiveKind.Users));
        Assert.Equal(0, RegistryMount.HiveHandle(RegistryHiveKind.CurrentUser));

        var mount = new RegistryMount("file", RegistryHiveKind.Users, "VestDemo");
        Assert.Equal(RegistryWriteStatus.Denied, mount.Dismount(confirm: false).Status);
        var second = mount.Dismount(confirm: true);
        Assert.True(second.Status is RegistryWriteStatus.Denied or RegistryWriteStatus.Ok);
        Assert.Equal(RegistryWriteStatus.Ok, mount.Dismount(confirm: true).Status);
        mount.Dispose();
    }

    [Fact]
    public void Import_export_search_edges()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "no-reg-vest.reg");
        var missing = Record.Exception(() => RegistryHelper.Import(missingPath, confirm: false));
        Assert.True(missing is FileNotFoundException or ArgumentException);

        var hits = RegistryHelper.Search(RegistryHiveKind.CurrentUser, "Software", "VestigiumNoHit", RegistrySearchMode.Contains, maxDepth: 1, maxResults: 4);
        Assert.NotNull(hits);
    }
}
