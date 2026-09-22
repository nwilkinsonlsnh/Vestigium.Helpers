using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryPhase2Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;

    public RegistryPhase2Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        var created = RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
        Assert.Equal(RegistryWriteStatus.Ok, created.Status);
    }

    public void Dispose()
        => RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);

    [Fact]
    public void SetValue_without_confirm_does_not_write()
    {
        var denied = RegistryHelper.Local.SetValue(Hive, _root, "Secret", "payload-should-not-land", confirm: false);
        Assert.Equal(RegistryWriteStatus.Denied, denied.Status);
        Assert.Null(RegistryHelper.Local.GetValue(Hive, _root, "Secret"));
    }

    [Fact]
    public void SetValue_round_trips_kinds()
    {
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "S", "hello", RegistryValueKind.String, confirm: true).Status);
        Assert.Equal("hello", RegistryHelper.Local.GetValue(Hive, _root, "S")?.Data);

        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "D", 42, RegistryValueKind.DWord, confirm: true).Status);
        Assert.Equal(42, RegistryHelper.Local.GetValue(Hive, _root, "D")?.Data);

        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "Q", 99L, RegistryValueKind.QWord, confirm: true).Status);
        Assert.Equal(99L, RegistryHelper.Local.GetValue(Hive, _root, "Q")?.Data);

        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "M", new[] { "a", "b" }, RegistryValueKind.MultiString, confirm: true).Status);
        Assert.Equal(new[] { "a", "b" }, RegistryHelper.Local.GetValue(Hive, _root, "M")?.Data);

        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "B", new byte[] { 1, 2 }, RegistryValueKind.Binary, confirm: true).Status);
        Assert.Equal(new byte[] { 1, 2 }, RegistryHelper.Local.GetValue(Hive, _root, "B")?.Data);

        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "E", "%TEMP%", RegistryValueKind.ExpandString, confirm: true).Status);
        var expand = RegistryHelper.Local.GetValue(Hive, _root, "E", expand: false);
        Assert.Equal("%TEMP%", expand?.Data);
    }

    [Fact]
    public void SetValue_type_mismatch_is_rejected()
    {
        var result = RegistryHelper.Local.SetValue(Hive, _root, "Bad", "nope", RegistryValueKind.DWord, confirm: true);
        Assert.Equal(RegistryWriteStatus.TypeMismatch, result.Status);
    }

    [Fact]
    public void DeleteValue_removes_name()
    {
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "Gone", "x", confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.DeleteValue(Hive, _root, "Gone", confirm: true).Status);
        Assert.Null(RegistryHelper.Local.GetValue(Hive, _root, "Gone"));
    }

    [Fact]
    public void DeleteKey_non_recursive_refuses_children()
    {
        var child = _root + @"\child";
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.CreateKey(Hive, child, confirm: true).Status);
        var result = RegistryHelper.Local.DeleteKey(Hive, _root, recursive: false, confirm: true);
        Assert.Equal(RegistryWriteStatus.InvalidPath, result.Status);
        Assert.NotNull(RegistryHelper.Local.GetKey(Hive, child));
    }

    [Fact]
    public void Forbidden_hklm_system_is_denied()
    {
        var result = RegistryHelper.Local.SetValue(RegistryHiveKind.LocalMachine, @"SYSTEM\CurrentControlSet", "VestigiumNo", "x", confirm: true);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
    }
}
