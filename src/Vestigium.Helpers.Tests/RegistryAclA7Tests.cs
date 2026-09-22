using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryAclA7Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;

    public RegistryAclA7Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        RegistryHelper.Local.CreateKey(Hive, _root + @"\Old\Child", confirm: true);
        RegistryHelper.Local.SetValue(Hive, _root + @"\Old", "Mark", "moved", confirm: true);
    }

    public void Dispose()
        => RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);

    [Fact]
    public void Same_parent_rename_leaves_only_dest()
    {
        var src = _root + @"\Old";
        var dest = _root + @"\New";
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.RenameKey(Hive, src, dest, confirm: true).Status);
        Assert.Null(RegistryHelper.Local.GetKey(Hive, src));
        var snap = RegistryHelper.Local.GetKey(Hive, dest, level: RegistryDetailLevel.Full);
        Assert.NotNull(snap);
        Assert.Equal("moved", snap!.Values.First(v => v.Name == "Mark").DataText);
        Assert.Contains("Child", snap.SubKeyNames);
    }

    [Fact]
    public void Dest_exists_is_in_use_and_source_stays()
    {
        var src = _root + @"\Old";
        var dest = _root + @"\Taken";
        RegistryHelper.Local.CreateKey(Hive, dest, confirm: true);
        var result = RegistryHelper.RenameKey(Hive, src, dest, confirm: true);
        Assert.Equal(RegistryWriteStatus.InUse, result.Status);
        Assert.NotNull(RegistryHelper.Local.GetKey(Hive, src));
        Assert.NotNull(RegistryHelper.Local.GetKey(Hive, dest));
    }
}
