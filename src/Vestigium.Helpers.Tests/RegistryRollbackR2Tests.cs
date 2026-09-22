using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryRollbackR2Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _journalPath;

    public RegistryRollbackR2Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _journalPath = Path.Combine(Path.GetTempPath(), "vest-r2-" + Guid.NewGuid().ToString("N") + ".jnl");
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { File.Delete(_journalPath); } catch { /* best effort */ }
    }

    [Fact]
    public void SetValue_without_journal_does_not_create_file()
    {
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "A", "1", confirm: true).Status);
        Assert.False(File.Exists(_journalPath));
    }

    [Fact]
    public void SetValue_and_DeleteValue_append_muts()
    {
        using (var journal = RegistryHelper.CreateJournal(_journalPath, confirm: true, out _))
        {
            Assert.NotNull(journal);
            Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "A", "1", confirm: true, journal: journal).Status);
            Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "A", "2", confirm: true, journal: journal).Status);
            Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.DeleteValue(Hive, _root, "A", confirm: true, journal: journal).Status);
            journal!.CommitBatch();
        }

        var info = RegistryHelper.ReadJournal(_journalPath);
        Assert.Equal(3, info.Batches[0].Mutations);
        var text = File.ReadAllText(_journalPath);
        Assert.Contains("SetValue", text, StringComparison.Ordinal);
        Assert.Contains("DeleteValue", text, StringComparison.Ordinal);
        Assert.Contains("beforePayload", text, StringComparison.Ordinal);
    }
}
