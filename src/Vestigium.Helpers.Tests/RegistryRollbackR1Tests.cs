using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

public sealed class RegistryRollbackR1Tests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "vest-jnl-" + Guid.NewGuid().ToString("N"));

    public RegistryRollbackR1Tests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    [Fact]
    public void Create_without_confirm_does_not_write()
    {
        var path = Path.Combine(_dir, "a.jnl");
        var journal = RegistryHelper.CreateJournal(path, confirm: false, out var result);
        Assert.Null(journal);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Create_load_and_batch_round_trip()
    {
        var path = Path.Combine(_dir, "b.jnl");
        using (var journal = RegistryHelper.CreateJournal(path, confirm: true, out var created))
        {
            Assert.Equal(RegistryWriteStatus.Ok, created.Status);
            Assert.NotNull(journal);
            Assert.False(journal!.Protect);
            Assert.Equal(RegistryWriteStatus.Ok, journal.BeginBatch("import", "demo.reg").Status);
            Assert.Equal(RegistryWriteStatus.Ok, journal.CommitBatch().Status);
        }

        var info = RegistryHelper.ReadJournal(path);
        Assert.Equal(RegistryJournal.Schema, info.Schema);
        Assert.Single(info.Batches);
        Assert.Equal("import", info.Batches[0].Kind);
        Assert.Equal("committed", info.Batches[0].Status);

        using var loaded = RegistryHelper.LoadJournal(path, confirm: true, out var loadedResult);
        Assert.Equal(RegistryWriteStatus.Ok, loadedResult.Status);
        Assert.NotNull(loaded);
    }

    [Fact]
    public void Create_existing_is_in_use()
    {
        var path = Path.Combine(_dir, "c.jnl");
        using (RegistryHelper.CreateJournal(path, confirm: true, out _)) { }
        using var again = RegistryHelper.CreateJournal(path, confirm: true, out var result);
        Assert.Null(again);
        Assert.Equal(RegistryWriteStatus.InUse, result.Status);
    }
}
