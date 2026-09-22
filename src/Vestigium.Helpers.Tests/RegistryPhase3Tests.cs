using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryPhase3Tests
{
    [Fact]
    public void For_dot_and_localhost_are_local()
    {
        Assert.True(RegistryHelper.For(".").IsLocal);
        Assert.True(RegistryHelper.For("localhost").IsLocal);
        Assert.True(RegistryHelper.For(Environment.MachineName).IsLocal);
        Assert.Null(RegistryHelper.For(".").Machine);
    }

    [Fact]
    public void CanConnect_local_is_true()
        => Assert.True(RegistryHelper.CanConnect("."));

    [Fact]
    public void For_dot_lists_hkcu_software()
        => Assert.NotEmpty(RegistryHelper.For(".").ListSubKeys(RegistryHiveKind.CurrentUser, "Software", level: RegistryDetailLevel.Identity));

    [Fact]
    public void CanConnect_missing_host_is_false()
    {
        var previous = RegistryHelper.ConnectTimeout;
        RegistryHelper.ConnectTimeout = TimeSpan.FromSeconds(2);
        try
        {
            Assert.False(RegistryHelper.CanConnect("no-such-host-vestigium-xyz", out var reason));
            Assert.False(string.IsNullOrWhiteSpace(reason));
        }
        finally
        {
            RegistryHelper.ConnectTimeout = previous;
        }
    }

    [Fact]
    public void Missing_host_get_is_null_and_write_is_denied()
    {
        var remote = RegistryHelper.For("no-such-host-vestigium-xyz");
        Assert.False(remote.IsLocal);
        Assert.Null(remote.GetKey(RegistryHiveKind.CurrentUser, "Software"));
        var write = remote.SetValue(RegistryHiveKind.CurrentUser, @"Software\Vestigium\Helpers.Tests\remote", "X", "1", confirm: true);
        Assert.Equal(RegistryWriteStatus.Denied, write.Status);
    }
}
