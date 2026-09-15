using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryRollbackR3Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryRollbackR3Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _dir = Path.Combine(Path.GetTempPath(), "vest-r3-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    [Fact]
    public void Import_with_journal_records_setvalue()
    {
        var dest = _root + @"\Imp";
        var reg = Path.Combine(_dir, "in.reg");
        var jnl = Path.Combine(_dir, "in.jnl");
        File.WriteAllText(reg, "Windows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\" + dest + "]\r\n\"Mark\"=\"one\"\r\n");
        using (var journal = RegistryHelper.CreateJournal(jnl, confirm: true, out _))
        {
            Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Import(reg, journal!, confirm: true).Status);
            journal!.CommitBatch();
        }

        Assert.Equal("one", RegistryHelper.Local.GetValue(Hive, dest, "Mark")?.DataText);
        Assert.Contains("SetValue", File.ReadAllText(jnl), StringComparison.Ordinal);
    }

    [Fact]
    public void Restore_index_without_payload_skips_values()
    {
        var src = _root + @"\Idx";
        RegistryHelper.Local.SetValue(Hive, src, "Mark", "keep", confirm: true);
        var index = Path.Combine(_dir, "idx.jsonl");
        var jnl = Path.Combine(_dir, "idx.jnl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(index, Hive, src, confirm: true).Status);
        var result = RegistryHelper.Restore(index, confirm: true, journalPath: jnl);
        Assert.Equal(RegistryWriteStatus.Ok, result.Status);
        Assert.Contains("skipped=", result.Reason, StringComparison.Ordinal);
    }
}
