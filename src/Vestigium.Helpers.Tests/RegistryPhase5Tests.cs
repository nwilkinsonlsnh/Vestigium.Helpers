using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryPhase5Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryPhase5Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _dir = Path.Combine(Path.GetTempPath(), "vest-winreg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.CreateKey(Hive, _root, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "Mark", "keep", confirm: true).Status);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Import_without_confirm_does_not_write()
    {
        var file = Path.Combine(_dir, "skip.reg");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Export(file, Hive, _root, confirm: true).Status);
        RegistryHelper.Local.DeleteValue(Hive, _root, "Mark", confirm: true);
        var result = RegistryHelper.Import(file, confirm: false);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
        Assert.Null(RegistryHelper.Local.GetValue(Hive, _root, "Mark"));
    }

    [Fact]
    public void Export_then_import_restores_value()
    {
        var file = Path.Combine(_dir, "round.reg");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Export(file, Hive, _root, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.DeleteValue(Hive, _root, "Mark", confirm: true).Status);
        var result = RegistryHelper.Import(file, confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, result.Status);
        Assert.Equal("keep", RegistryHelper.Local.GetValue(Hive, _root, "Mark")?.Data);
    }

    [Fact]
    public void Import_delete_value_line()
    {
        var file = Path.Combine(_dir, "del.reg");
        File.WriteAllText(file, "Windows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\" + _root + "]\r\n\"Mark\"=-\r\n");
        var result = RegistryHelper.Import(file, confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, result.Status);
        Assert.Null(RegistryHelper.Local.GetValue(Hive, _root, "Mark"));
    }

    [Fact]
    public void Import_bad_line_reports_line_number()
    {
        var file = Path.Combine(_dir, "bad.reg");
        File.WriteAllText(file, "Windows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\" + _root + "]\r\nthis is not a value\r\n");
        var result = RegistryHelper.Import(file, confirm: true);
        Assert.Equal(RegistryWriteStatus.InvalidPath, result.Status);
        Assert.Contains("line ", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
