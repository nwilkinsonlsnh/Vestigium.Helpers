using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryCompareC3Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryCompareC3Tests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vest-regc3-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root, "Keep", "same", confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root, "Gone", "left", confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Merge_writes_changed_left_and_right()
    {
        var a = Path.Combine(_dir, "a.jsonl");
        var b = Path.Combine(_dir, "b.jsonl");
        var outFile = Path.Combine(_dir, "cmp.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(a, Hive, _root, confirm: true).Status);
        RegistryHelper.Local.SetValue(Hive, _root, "Keep", "changed", confirm: true);
        RegistryHelper.Local.DeleteValue(Hive, _root, "Gone", confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root, "New", "right", confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(b, Hive, _root, confirm: true).Status);

        var result = RegistryHelper.Compare(a, b, outFile, confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, result.Status);
        var text = File.ReadAllText(outFile);
        Assert.Contains("\"kind\":\"Changed\"", text);
        Assert.Contains("\"kind\":\"LeftOnly\"", text);
        Assert.Contains("\"kind\":\"RightOnly\"", text);
        Assert.DoesNotContain("\"kind\":\"Same\"", text);
        Assert.DoesNotContain("changed-payload-not-this", text);
        Assert.Contains("\"stopped\":false", text);
    }

    [Fact]
    public void IncludeSame_writes_same_rows()
    {
        var a = Path.Combine(_dir, "a2.jsonl");
        var outFile = Path.Combine(_dir, "cmp2.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(a, Hive, _root, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Compare(a, a, outFile, confirm: true, includeSame: true).Status);
        Assert.Contains("\"kind\":\"Same\"", File.ReadAllText(outFile));
    }
}
