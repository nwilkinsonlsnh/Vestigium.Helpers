using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryRollbackR5Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryRollbackR5Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _dir = Path.Combine(Path.GetTempPath(), "vest-r5-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root, "Keep", "old", confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    [Fact]
    public void Rollback_restores_previous_value()
    {
        var journal = Path.Combine(_dir, "a.jnl");
        var edits = new RegistryEditList()
            .SetValue(Hive, _root, "Keep", "new")
            .SetValue(Hive, _root, "Fresh", "x")
            .DeleteValue(Hive, _root, "Keep");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Apply(edits, journal, confirm: true).Status);
        Assert.Null(RegistryHelper.Local.GetValue(Hive, _root, "Keep"));
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Rollback(journal, confirm: true).Status);
        Assert.Equal("old", RegistryHelper.Local.GetValue(Hive, _root, "Keep")?.DataText);
        Assert.Null(RegistryHelper.Local.GetValue(Hive, _root, "Fresh"));
    }

    [Fact]
    public void Rollback_skips_collided_value()
    {
        var journal = Path.Combine(_dir, "b.jnl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Apply(
            new RegistryEditList().SetValue(Hive, _root, "Keep", "new"),
            journal, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "Keep", "other", confirm: true).Status);
        var result = RegistryHelper.Rollback(journal, confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, result.Status);
        Assert.Contains("skipped=", result.Reason);
        Assert.Equal("other", RegistryHelper.Local.GetValue(Hive, _root, "Keep")?.DataText);
    }
}
