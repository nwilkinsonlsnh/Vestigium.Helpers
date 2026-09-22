using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryRollbackR6Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryRollbackR6Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _dir = Path.Combine(Path.GetTempPath(), "vest-r6-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        RegistryHelper.Local.CreateKey(Hive, _root + @"\Src", confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root + @"\Src", "Mark", "keep", confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    [Fact]
    public void Copy_then_rollback_removes_dest()
    {
        var journalPath = Path.Combine(_dir, "copy.jnl");
        using (var journal = RegistryHelper.CreateJournal(journalPath, confirm: true, out _))
        {
            var dest = _root + @"\Copied";
            Assert.Equal(RegistryWriteStatus.Ok,
                RegistryHelper.Local.CopyKey(Hive, _root + @"\Src", Hive, dest, confirm: true, journal: journal).Status);
            Assert.NotNull(RegistryHelper.Local.GetKey(Hive, dest));
        }

        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Rollback(journalPath, confirm: true).Status);
        Assert.Null(RegistryHelper.Local.GetKey(Hive, _root + @"\Copied"));
        Assert.NotNull(RegistryHelper.Local.GetKey(Hive, _root + @"\Src"));
    }

    [Fact]
    public void Rename_then_rollback_restores_name()
    {
        var journalPath = Path.Combine(_dir, "ren.jnl");
        var src = _root + @"\Src";
        var dest = _root + @"\Renamed";
        using (var journal = RegistryHelper.CreateJournal(journalPath, confirm: true, out _))
        {
            Assert.Equal(RegistryWriteStatus.Ok,
                RegistryHelper.Local.RenameKey(Hive, src, dest, confirm: true, journal: journal).Status);
        }

        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Rollback(journalPath, confirm: true).Status);
        Assert.NotNull(RegistryHelper.Local.GetKey(Hive, src));
        Assert.Null(RegistryHelper.Local.GetKey(Hive, dest));
    }
}
