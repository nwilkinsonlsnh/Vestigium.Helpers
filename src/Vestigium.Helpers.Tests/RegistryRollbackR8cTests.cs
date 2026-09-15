using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryRollbackR8cTests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryRollbackR8cTests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _dir = Path.Combine(Path.GetTempPath(), "vest-r8c-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    [Fact]
    public void Archive_keeps_dropped_batch()
    {
        var journal = Path.Combine(_dir, "live.jnl");
        var archive = Path.Combine(_dir, "drop.jnl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Apply(new RegistryEditList().SetValue(Hive, _root, "A", "1"), journal, confirm: true).Status);
        using (var open = RegistryHelper.LoadJournal(journal, confirm: true, out _))
        {
            open!.BeginBatch("edits", "two");
            RegistryHelper.Local.SetValue(Hive, _root, "B", "2", confirm: true, journal: open);
            open.CommitBatch();
        }

        var result = RegistryHelper.PurgeJournal(journal, new RegistryPurgeOptions
        {
            KeepLastBatches = 1,
            ArchivePath = archive,
            Confirm = true
        });
        Assert.Equal(RegistryWriteStatus.Ok, result.Status);
        Assert.True(File.Exists(archive));
        Assert.Contains("SetValue", File.ReadAllText(archive), StringComparison.Ordinal);
        Assert.Equal(1, RegistryHelper.ReadJournal(journal).Batches.Count);
        Assert.True(RegistryHelper.ReadJournal(archive).Batches.Count >= 1);
    }
}
