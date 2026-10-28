using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryRollbackR8Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryRollbackR8Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _dir = Path.Combine(Path.GetTempPath(), "vest-r8-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    [Fact]
    public void Keep_last_batch_drops_the_first()
    {
        var journal = Path.Combine(_dir, "k.jnl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Apply(new RegistryEditList().SetValue(Hive, _root, "A", "1"), journal, confirm: true).Status);
        using (var open = RegistryHelper.LoadJournal(journal, confirm: true, out _))
        {
            open!.BeginBatch("edits", "two");
            RegistryHelper.Local.SetValue(Hive, _root, "B", "2", confirm: true, journal: open);
            open.CommitBatch();
        }

        var dry = RegistryHelper.PurgeJournal(journal, new RegistryPurgeOptions { KeepLastBatches = 1, DryRun = true });
        Assert.Equal(RegistryWriteStatus.Ok, dry.Status);
        Assert.True(dry.DryRun);
        Assert.Equal(1, dry.BatchesKept);
        Assert.Equal(1, dry.BatchesRemoved);

        var live = RegistryHelper.PurgeJournal(journal, new RegistryPurgeOptions { KeepLastBatches = 1, Confirm = true });
        Assert.Equal(RegistryWriteStatus.Ok, live.Status);
        Assert.Single(RegistryHelper.ReadJournal(journal).Batches);
        Assert.Equal("two", RegistryHelper.ReadJournal(journal).Batches[0].Label);
    }

    [Fact]
    public void Compact_without_confirm_is_denied()
    {
        var journal = Path.Combine(_dir, "n.jnl");
        using (RegistryHelper.CreateJournal(journal, confirm: true, out _)) { }
        var result = RegistryHelper.PurgeJournal(journal, new RegistryPurgeOptions { KeepLastBatches = 1 });
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
    }
}
