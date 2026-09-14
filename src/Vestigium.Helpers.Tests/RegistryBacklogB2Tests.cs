using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryBacklogB2Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryBacklogB2Tests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vest-regb2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root, "Mark", "b2", confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Client_write_index_matches_helper()
    {
        var a = Path.Combine(_dir, "a.jsonl");
        var b = Path.Combine(_dir, "b.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.WriteIndex(a, Hive, _root, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.For(".").WriteIndex(b, Hive, _root, confirm: true).Status);
        Assert.Contains("vest-regidx/1", File.ReadAllText(a));
        Assert.Contains("vest-regidx/1", File.ReadAllText(b));
    }

    [Fact]
    public void Canceled_export_is_denied()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = RegistryHelper.Export(Path.Combine(_dir, "x.reg"), Hive, _root, confirm: true, cancel: cts.Token);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
        Assert.Equal("canceled", result.Reason);
    }

    [Fact]
    public void Canceled_import_is_denied()
    {
        var file = Path.Combine(_dir, "t.reg");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Export(file, Hive, _root, confirm: true).Status);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = RegistryHelper.Import(file, confirm: true, cancel: cts.Token);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
    }
}
