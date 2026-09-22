using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryPhase4Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryPhase4Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _dir = Path.Combine(Path.GetTempPath(), "vest-winreg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.CreateKey(Hive, _root, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "Mark", "phase4", confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "Number", 42, RegistryValueKind.DWord, confirm: true).Status);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Export_without_confirm_does_not_create_file()
    {
        var file = Path.Combine(_dir, "no.reg");
        var result = RegistryHelper.Export(file, Hive, _root, confirm: false);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
        Assert.False(File.Exists(file));
    }

    [Fact]
    public void Export_missing_key_is_not_found()
    {
        var file = Path.Combine(_dir, "missing.reg");
        var result = RegistryHelper.Export(file, Hive, _root + @"\gone", confirm: true);
        Assert.Equal(RegistryWriteStatus.NotFound, result.Status);
    }

    [Fact]
    public void Export_regfile_has_header_and_value()
    {
        var file = Path.Combine(_dir, "out.reg");
        var result = RegistryHelper.Export(file, Hive, _root, RegistryExportFormat.RegFile, confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, result.Status);
        var text = File.ReadAllText(file);
        Assert.StartsWith("Windows Registry Editor Version 5.00", text);
        Assert.Contains("Mark", text);
        Assert.Contains("phase4", text);
        Assert.Contains("dword:0000002a", text);
    }

    [Fact]
    public void Export_hivefile_is_non_empty_or_denied()
    {
        var file = Path.Combine(_dir, "out.hiv");
        var result = RegistryHelper.Export(file, Hive, _root, RegistryExportFormat.HiveFile, confirm: true);
        Assert.True(result.Status is RegistryWriteStatus.Ok or RegistryWriteStatus.Denied);
        if (result.Status == RegistryWriteStatus.Ok)
            Assert.True(new FileInfo(file).Length > 0);
    }
}
