using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryRollbackR4Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryRollbackR4Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _dir = Path.Combine(Path.GetTempPath(), "vest-r4-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root, "OldName", "keep", confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root, "Gone", "x", confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    [Fact]
    public void Apply_list_renames_and_deletes()
    {
        var edits = new RegistryEditList()
            .SetValue(Hive, _root, "DisplayName", "New")
            .RenameValue(Hive, _root, "OldName", "NewName")
            .DeleteValue(Hive, _root, "Gone");
        var journal = Path.Combine(_dir, "apply.jnl");
        var recipe = Path.Combine(_dir, "edits.json");
        edits.Save(recipe);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Apply(recipe, journal, confirm: true).Status);
        Assert.Equal("New", RegistryHelper.Local.GetValue(Hive, _root, "DisplayName")?.DataText);
        Assert.Equal("keep", RegistryHelper.Local.GetValue(Hive, _root, "NewName")?.DataText);
        Assert.Null(RegistryHelper.Local.GetValue(Hive, _root, "OldName"));
        Assert.Null(RegistryHelper.Local.GetValue(Hive, _root, "Gone"));
        Assert.Contains("RenameValue", File.ReadAllText(journal) + File.ReadAllText(recipe), StringComparison.Ordinal);
    }

    [Fact]
    public void Apply_without_confirm_does_not_write()
    {
        var journal = Path.Combine(_dir, "no.jnl");
        var result = RegistryHelper.Apply(new RegistryEditList().SetValue(Hive, _root, "X", "1"), journal, confirm: false);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
        Assert.False(File.Exists(journal));
    }
}
