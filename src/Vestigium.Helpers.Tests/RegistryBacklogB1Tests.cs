using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryBacklogB1Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryBacklogB1Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _dir = Path.Combine(Path.GetTempPath(), "vest-b1-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        RegistryHelper.Local.CreateKey(Hive, _root + @"\Child", confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root + @"\Child", "Mark", "keep", confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    [Fact]
    public void DeleteKey_rollback_restores_tree()
    {
        var journal = Path.Combine(_dir, "d.jnl");
        using (var open = RegistryHelper.CreateJournal(journal, confirm: true, out _))
        {
            Assert.Equal(RegistryWriteStatus.Ok,
                RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true, journal: open).Status);
            open!.CommitBatch();
        }
        Assert.Null(RegistryHelper.Local.GetKey(Hive, _root));
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Rollback(journal, confirm: true).Status);
        Assert.Equal("keep", RegistryHelper.Local.GetValue(Hive, _root + @"\Child", "Mark")?.DataText);
    }

    [Fact]
    public void Compact_while_open_is_in_use()
    {
        var journal = Path.Combine(_dir, "o.jnl");
        using (RegistryHelper.CreateJournal(journal, confirm: true, out _))
        {
            var result = RegistryHelper.PurgeJournal(journal, new RegistryPurgeOptions { KeepLastBatches = 1, Confirm = true });
            Assert.Equal(RegistryWriteStatus.InUse, result.Status);
        }
    }

    [Fact]
    public void Restore_index_without_payload_is_unsupported()
    {
        var index = Path.Combine(_dir, "i.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(index, Hive, _root, confirm: true).Status);
        var result = RegistryHelper.Restore(index, confirm: true, journalPath: Path.Combine(_dir, "r.jnl"));
        Assert.Equal(RegistryWriteStatus.Unsupported, result.Status);
        Assert.Contains("no payloads", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
