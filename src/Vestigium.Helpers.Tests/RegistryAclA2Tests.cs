using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryAclA2Tests
{
    [Fact]
    public void Privilege_scope_on_failed_dismount_does_not_throw()
    {
        var result = RegistryHelper.DismountHive(RegistryHiveKind.LocalMachine, "VESTIGIUM_NO_SUCH_MOUNT_A2", confirm: true);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
        Assert.Equal(RegistryWriteStatus.Denied, RegistryHelper.DismountHive(RegistryHiveKind.LocalMachine, "VESTIGIUM_NO_SUCH_MOUNT_A2", confirm: true).Status);
    }
}
