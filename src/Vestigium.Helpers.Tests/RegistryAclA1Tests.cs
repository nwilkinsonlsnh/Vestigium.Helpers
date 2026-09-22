using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryAclA1Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;

    public RegistryAclA1Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        RegistryHelper.Local.CreateKey(Hive, _root + @"\Src\Child", confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root + @"\Src", "Mark", "keep", confirm: true);
    }

    public void Dispose()
        => RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);

    [Fact]
    public void Helper_copy_without_confirm_does_not_write()
    {
        var dest = _root + @"\No";
        Assert.Equal(RegistryWriteStatus.Denied, RegistryHelper.CopyKey(Hive, _root + @"\Src", Hive, dest, confirm: false).Status);
        Assert.Null(RegistryHelper.Local.GetKey(Hive, dest));
    }

    [Fact]
    public void Helper_copy_and_rename_move_values()
    {
        var copyTo = _root + @"\Copied";
        var renamed = _root + @"\Renamed";
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.CopyKey(Hive, _root + @"\Src", Hive, copyTo, confirm: true).Status);
        Assert.Equal("keep", RegistryHelper.Local.GetValue(Hive, copyTo, "Mark")?.DataText);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.RenameKey(Hive, copyTo, renamed, confirm: true).Status);
        Assert.Null(RegistryHelper.Local.GetKey(Hive, copyTo));
        Assert.NotNull(RegistryHelper.Local.GetKey(Hive, renamed));
    }

    [Fact]
    public void Canceled_copy_does_not_leave_dest()
    {
        var dest = _root + @"\Canceled";
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = RegistryHelper.CopyKey(Hive, _root + @"\Src", Hive, dest, confirm: true, cancel: cts.Token);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
        Assert.Null(RegistryHelper.Local.GetKey(Hive, dest));
        Assert.NotNull(RegistryHelper.Local.GetKey(Hive, _root + @"\Src"));
    }
}
