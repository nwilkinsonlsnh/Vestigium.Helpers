using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryBacklogB3Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryBacklogB3Tests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vest-regb3-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root, "Mark", "alpha", confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void CompareDetailed_counts_changed()
    {
        var a = Path.Combine(_dir, "a.jsonl");
        var b = Path.Combine(_dir, "b.jsonl");
        var output = Path.Combine(_dir, "c.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(a, Hive, _root, confirm: true).Status);
        RegistryHelper.Local.SetValue(Hive, _root, "Mark", "beta", confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(b, Hive, _root, confirm: true).Status);
        var summary = RegistryHelper.CompareDetailed(a, b, output, confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, summary.Status);
        Assert.True(summary.Changed >= 1);
        Assert.Equal(summary.Changed, File.ReadAllLines(output).Count(l => l.Contains("\"kind\":\"Changed\"")));
    }

    [Fact]
    public void IncludePayload_puts_text_on_changed_only()
    {
        var a = Path.Combine(_dir, "a2.jsonl");
        var b = Path.Combine(_dir, "b2.jsonl");
        var output = Path.Combine(_dir, "c2.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(a, Hive, _root, confirm: true, includePayload: true).Status);
        RegistryHelper.Local.SetValue(Hive, _root, "Mark", "beta", confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(b, Hive, _root, confirm: true, includePayload: true).Status);
        var summary = RegistryHelper.CompareDetailed(a, b, output, confirm: true, includePayload: true);
        var changed = summary.Deltas.Single(d => d.Kind == "Changed" && d.Name == "Mark");
        Assert.Equal("alpha", changed.LeftText);
        Assert.Equal("beta", changed.RightText);
        Assert.Contains("\"leftText\":\"alpha\"", File.ReadAllText(output));
    }
}
