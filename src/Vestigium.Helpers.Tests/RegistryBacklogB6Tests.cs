using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryBacklogB6Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryBacklogB6Tests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vest-regb6-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        RegistryHelper.Local.CreateKey(Hive, _root + @"\Child", confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root, "Mark", "copy-me", confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Import_allowedHives_rejects_other_hive()
    {
        var reg = Path.Combine(_dir, "t.reg");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Export(reg, Hive, _root, confirm: true).Status);
        var result = RegistryHelper.Import(reg, confirm: true, allowedHives: [RegistryHiveKind.LocalMachine]);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
        Assert.Contains("not allowed", result.Reason);
    }

    [Fact]
    public void Copy_and_rename_move_values()
    {
        var copyTo = _root + @"\Copied";
        var renamed = _root + @"\Renamed";
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.CopyKey(Hive, _root, Hive, copyTo, confirm: true).Status);
        Assert.Equal("copy-me", RegistryHelper.Local.GetValue(Hive, copyTo, "Mark")?.DataText);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.RenameKey(Hive, copyTo, renamed, confirm: true).Status);
        Assert.Null(RegistryHelper.Local.GetKey(Hive, copyTo));
        Assert.Equal("copy-me", RegistryHelper.Local.GetValue(Hive, renamed, "Mark")?.DataText);
    }

    [Fact]
    public void Search_canceled_returns_without_throw()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var hits = RegistryHelper.Search(Hive, _root, "Mark", cancel: cts.Token);
        Assert.NotNull(hits);
    }
}
