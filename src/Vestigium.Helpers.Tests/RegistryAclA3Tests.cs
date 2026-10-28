using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryAclA3Tests
{
    [Fact]
    public void IndexFromHive_without_confirm_does_not_mount()
    {
        var dest = Path.Combine(Path.GetTempPath(), "vest-hiv-" + Guid.NewGuid().ToString("N") + ".jsonl");
        var result = RegistryHelper.IndexFromHive("no-such.hiv", dest, RegistryHiveKind.LocalMachine, "VESTIGIUM_A3", confirm: false);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
        Assert.False(File.Exists(dest));
    }

    [Fact]
    public void IndexFromHive_missing_file_is_not_found()
    {
        var dest = Path.Combine(Path.GetTempPath(), "vest-hiv-" + Guid.NewGuid().ToString("N") + ".jsonl");
        var missing = Path.Combine(Path.GetTempPath(), "vest-missing-" + Guid.NewGuid().ToString("N") + ".hiv");
        var result = RegistryHelper.IndexFromHive(missing, dest, RegistryHiveKind.LocalMachine, "VESTIGIUM_A3", confirm: true);
        Assert.Equal(RegistryWriteStatus.NotFound, result.Status);
        Assert.False(File.Exists(dest));
    }

    [Fact]
    public void IndexFromHive_rejects_hkcu_destination()
    {
        var dest = Path.Combine(Path.GetTempPath(), "vest-hiv-" + Guid.NewGuid().ToString("N") + ".jsonl");
        var result = RegistryHelper.IndexFromHive("x.hiv", dest, RegistryHiveKind.CurrentUser, "VESTIGIUM_A3", confirm: true);
        Assert.Equal(RegistryWriteStatus.Unsupported, result.Status);
    }
}
