using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryCompareC2Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _left;
    private readonly string _other;
    private readonly string _dir;

    public RegistryCompareC2Tests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vest-regcmp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _left = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _other = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        RegistryHelper.Local.CreateKey(Hive, _left + @"\Child", confirm: true);
        RegistryHelper.Local.SetValue(Hive, _left, "Mark", "same", confirm: true);
        RegistryHelper.Local.CreateKey(Hive, _other, confirm: true);
        RegistryHelper.Local.SetValue(Hive, _other, "Other", "x", confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _left, recursive: true, confirm: true);
        RegistryHelper.Local.DeleteKey(Hive, _other, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Same_shape_is_related_and_has_no_delta_lines()
    {
        var a = Path.Combine(_dir, "a.jsonl");
        var b = Path.Combine(_dir, "b.jsonl");
        var outFile = Path.Combine(_dir, "cmp.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(a, Hive, _left, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(b, Hive, _left, confirm: true).Status);
        var result = RegistryHelper.Compare(a, b, outFile, confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, result.Status);
        var text = File.ReadAllText(outFile);
        Assert.Contains("vest-regcmp/1", text);
        Assert.Contains("\"verdict\":\"Related\"", text);
        Assert.Contains("\"stopped\":false", text);
        Assert.DoesNotContain("\"rec\":\"delta\"", text);
    }

    [Fact]
    public void Different_roots_are_unrelated_and_stop()
    {
        var a = Path.Combine(_dir, "a2.jsonl");
        var b = Path.Combine(_dir, "b2.jsonl");
        var outFile = Path.Combine(_dir, "cmp2.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(a, Hive, _left, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(b, Hive, _other, confirm: true).Status);
        var result = RegistryHelper.Compare(a, b, outFile, confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, result.Status);
        var text = File.ReadAllText(outFile);
        Assert.Contains("\"verdict\":\"Unrelated\"", text);
        Assert.Contains("\"stopped\":true", text);
        Assert.DoesNotContain("\"rec\":\"delta\"", text);
    }

    [Fact]
    public void Compare_without_confirm_does_not_write()
    {
        var outFile = Path.Combine(_dir, "no.jsonl");
        var a = Path.Combine(_dir, "a3.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(a, Hive, _left, confirm: true).Status);
        var result = RegistryHelper.Compare(a, a, outFile, confirm: false);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
        Assert.False(File.Exists(outFile));
    }
}
