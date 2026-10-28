using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryBacklogB2Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryBacklogB2Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _dir = Path.Combine(Path.GetTempPath(), "vest-b2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root, "From", "data", confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    [Fact]
    public void ReadText_works_while_journal_open()
    {
        var path = Path.Combine(_dir, "t.jnl");
        using var journal = RegistryHelper.CreateJournal(path, confirm: true, out _);
        Assert.Contains("vest-regjnl/1", RegistryHelper.ReadJournalText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void RenameValue_is_one_mut_and_rolls_back()
    {
        var path = Path.Combine(_dir, "r.jnl");
        using (var journal = RegistryHelper.CreateJournal(path, confirm: true, out _))
        {
            Assert.Equal(RegistryWriteStatus.Ok,
                RegistryHelper.Local.RenameValue(Hive, _root, "From", "To", confirm: true, journal: journal).Status);
            journal!.CommitBatch();
        }

        var text = File.ReadAllText(path);
        Assert.Contains("RenameValue", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\"op\":\"SetValue\"", text, StringComparison.Ordinal);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Rollback(path, confirm: true).Status);
        Assert.Equal("data", RegistryHelper.Local.GetValue(Hive, _root, "From")?.DataText);
        Assert.Null(RegistryHelper.Local.GetValue(Hive, _root, "To"));
    }

    [Fact]
    public void Rollback_by_batch_id_leaves_later_batch()
    {
        var path = Path.Combine(_dir, "b.jnl");
        string first;
        using (var journal = RegistryHelper.CreateJournal(path, confirm: true, out _))
        {
            first = journal!.BeginBatch("edits", "one").Reason!;
            RegistryHelper.Local.SetValue(Hive, _root, "A", "1", confirm: true, journal: journal);
            journal.CommitBatch();
            journal.BeginBatch("edits", "two");
            RegistryHelper.Local.SetValue(Hive, _root, "B", "2", confirm: true, journal: journal);
            journal.CommitBatch();
        }

        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Rollback(path, confirm: true, batchId: first).Status);
        Assert.Null(RegistryHelper.Local.GetValue(Hive, _root, "A"));
        Assert.Equal("2", RegistryHelper.Local.GetValue(Hive, _root, "B")?.DataText);
    }
}
