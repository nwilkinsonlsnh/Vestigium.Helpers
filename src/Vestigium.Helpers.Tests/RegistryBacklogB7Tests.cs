using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryBacklogB7Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryBacklogB7Tests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vest-regb7-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root, "Keep", "same", confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Ignore_prefix_drops_noise_child()
    {
        var a = Path.Combine(_dir, "a.jsonl");
        var b = Path.Combine(_dir, "b.jsonl");
        var raw = Path.Combine(_dir, "raw.jsonl");
        var filtered = Path.Combine(_dir, "flt.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(a, Hive, _root, confirm: true).Status);
        RegistryHelper.Local.CreateKey(Hive, _root + @"\Noise", confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root + @"\Noise", "Vol", "only-right", confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(b, Hive, _root, confirm: true).Status);

        var without = RegistryHelper.CompareDetailed(a, b, raw, confirm: true);
        Assert.True(without.RightOnly >= 1);

        var with = RegistryHelper.CompareDetailed(a, b, filtered, confirm: true, ignorePathPrefixes: ["Noise"]);
        Assert.Equal(0, with.RightOnly);
        Assert.DoesNotContain(with.Deltas, d => d.Path.StartsWith("Noise", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("\"ignored\":", File.ReadAllText(filtered));
    }
}
