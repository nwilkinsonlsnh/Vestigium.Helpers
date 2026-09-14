using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryCompareC4Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _other;
    private readonly string _dir;

    public RegistryCompareC4Tests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vest-regc4-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _other = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root, "Mark", "c4-secret-payload", confirm: true);
        RegistryHelper.Local.CreateKey(Hive, _other, confirm: true);
        RegistryHelper.Local.SetValue(Hive, _other, "Other", "y", confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        RegistryHelper.Local.DeleteKey(Hive, _other, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Canceled_compare_is_denied()
    {
        var idx = Path.Combine(_dir, "i.jsonl");
        var output = Path.Combine(_dir, "out.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(idx, Hive, _root, confirm: true).Status);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = RegistryHelper.Compare(idx, idx, output, confirm: true, cancel: cts.Token);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
    }

    [Fact]
    public void Compare_file_is_not_an_index()
    {
        var idx = Path.Combine(_dir, "i2.jsonl");
        var cmp = Path.Combine(_dir, "c.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(idx, Hive, _root, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Compare(idx, idx, cmp, confirm: true).Status);
        var boom = Path.Combine(_dir, "boom.jsonl");
        Assert.Throws<ArgumentException>(() => RegistryHelper.Compare(cmp, idx, boom, confirm: true));
    }

    [Fact]
    public void Force_unrelated_sets_stopped_false()
    {
        var a = Path.Combine(_dir, "a.jsonl");
        var b = Path.Combine(_dir, "b.jsonl");
        var output = Path.Combine(_dir, "f.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(a, Hive, _root, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(b, Hive, _other, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Compare(a, b, output, confirm: true, force: true).Status);
        Assert.Contains("\"stopped\":false", File.ReadAllText(output));
    }

    [Fact]
    public void Index_and_compare_omit_payload()
    {
        var idx = Path.Combine(_dir, "i3.jsonl");
        var cmp = Path.Combine(_dir, "c3.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(idx, Hive, _root, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Compare(idx, idx, cmp, confirm: true, includeSame: true).Status);
        Assert.DoesNotContain("c4-secret-payload", File.ReadAllText(idx));
        Assert.DoesNotContain("c4-secret-payload", File.ReadAllText(cmp));
    }

    [Fact]
    public void Caps_are_documented()
    {
        Assert.Equal(2_000_000, RegistryHelper.MaxIndexValues);
        Assert.Equal(64, RegistryHelper.MaxIndexDepth);
    }
}
