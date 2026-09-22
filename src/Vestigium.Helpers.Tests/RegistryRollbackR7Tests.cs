using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryRollbackR7Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryRollbackR7Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _dir = Path.Combine(Path.GetTempPath(), "vest-r7-" + Guid.NewGuid().ToString("N"));
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
    public void Protect_journal_still_rolls_back()
    {
        var journal = Path.Combine(_dir, "p.jnl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Apply(
            new RegistryEditList().SetValue(Hive, _root, "Keep", "new"),
            journal, confirm: true, protect: true).Status);
        var raw = File.ReadAllText(journal);
        Assert.DoesNotContain("old", raw, StringComparison.Ordinal);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Rollback(journal, confirm: true).Status);
        Assert.Equal("old", RegistryHelper.Local.GetValue(Hive, _root, "Keep")?.DataText);
    }

    [Fact]
    public void Purge_requires_confirm_then_deletes()
    {
        var journal = Path.Combine(_dir, "g.jnl");
        using (RegistryHelper.CreateJournal(journal, confirm: true, out _)) { }
        Assert.Equal(RegistryWriteStatus.Denied, RegistryHelper.PurgeJournal(journal, confirm: false).Status);
        Assert.True(File.Exists(journal));
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.PurgeJournal(journal, confirm: true).Status);
        Assert.False(File.Exists(journal));
    }
}
