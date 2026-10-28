using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryAclA6Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;

    public RegistryAclA6Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
    }

    public void Dispose()
        => RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);

    [Fact]
    public void Acl_write_without_confirm_is_denied()
    {
        Assert.Equal(RegistryWriteStatus.Denied, RegistryHelper.TakeOwnership(Hive, _root, confirm: false).Status);
        Assert.Equal(RegistryWriteStatus.Denied, RegistryHelper.SetOwner(Hive, _root, @"NT AUTHORITY\SYSTEM", confirm: false).Status);
        Assert.Equal(RegistryWriteStatus.Denied, RegistryHelper.SetSddl(Hive, _root, "D:(A;;KA;;;WD)", confirm: false).Status);
    }

    [Fact]
    public void SetSddl_without_dacl_is_invalid()
    {
        var result = RegistryHelper.SetSddl(Hive, _root, "O:SY", confirm: true);
        Assert.Equal(RegistryWriteStatus.InvalidPath, result.Status);
    }

    [Fact]
    public void TakeOwnership_does_not_throw()
    {
        var result = RegistryHelper.TakeOwnership(Hive, _root, confirm: true);
        Assert.True(result.Status is RegistryWriteStatus.Ok or RegistryWriteStatus.Denied);
        if (result.Status == RegistryWriteStatus.Ok)
        {
            var snap = RegistryHelper.Local.GetKey(Hive, _root, level: RegistryDetailLevel.Full);
            Assert.False(string.IsNullOrWhiteSpace(snap?.Owner));
        }
    }
}
