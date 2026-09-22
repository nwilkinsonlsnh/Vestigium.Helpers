using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryBacklogB4Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryBacklogB4Tests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vest-regb4-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        RegistryHelper.Local.CreateKey(Hive, _root + @"\Child", confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root, "Mark", "from-reg", confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void IndexFromReg_without_confirm_does_not_write()
    {
        var dest = Path.Combine(_dir, "no.jsonl");
        var result = RegistryHelper.IndexFromReg(Path.Combine(_dir, "missing.reg"), dest, confirm: false);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
    }

    [Fact]
    public void IndexFromReg_of_export_compares_related_to_live_index()
    {
        var reg = Path.Combine(_dir, "tree.reg");
        var fromReg = Path.Combine(_dir, "from-reg.jsonl");
        var live = Path.Combine(_dir, "live.jsonl");
        var cmp = Path.Combine(_dir, "cmp.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Export(reg, Hive, _root, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.IndexFromReg(reg, fromReg, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(live, Hive, _root, confirm: true).Status);
        Assert.Contains("vest-regidx/1", File.ReadAllText(fromReg));
        Assert.Contains("\"source\":\"reg\"", File.ReadAllText(fromReg));
        var summary = RegistryHelper.CompareDetailed(live, fromReg, cmp, confirm: true);
        Assert.Equal("Related", summary.Verdict);
        Assert.False(summary.Stopped);
        Assert.True(summary.Same >= 1);
    }
}
