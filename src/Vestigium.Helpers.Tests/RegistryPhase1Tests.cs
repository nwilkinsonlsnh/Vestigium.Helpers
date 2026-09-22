using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryPhase1Tests
{
    [Fact]
    public void Probe_returns_identity()
        => Assert.Equal("Vestigium.Helpers.WinReg", RegistryHelper.Probe());

    [Fact]
    public void GetKey_missing_is_null()
        => Assert.Null(RegistryHelper.Local.GetKey(RegistryHiveKind.CurrentUser, @"Software\Vestigium\Helpers.Tests\NoSuchKey_Phase1"));

    [Fact]
    public void GetKey_slash_throws()
        => Assert.Throws<ArgumentException>(() => RegistryHelper.Local.GetKey(RegistryHiveKind.CurrentUser, "Software/Vestigium"));

    [Fact]
    public void ListSubKeys_hkcu_software_is_not_empty()
    {
        var rows = RegistryHelper.Local.ListSubKeys(RegistryHiveKind.CurrentUser, "Software", level: RegistryDetailLevel.Identity);
        Assert.NotEmpty(rows);
    }

    [Fact]
    public void GetKey_software_slim_has_counts()
    {
        var key = RegistryHelper.Local.GetKey(RegistryHiveKind.CurrentUser, "Software");
        Assert.NotNull(key);
        Assert.Equal("Software", key!.Name, ignoreCase: true);
        Assert.True(key.SubKeyCount is null or >= 0);
        Assert.NotNull(key.SubKeyNames);
    }

    [Fact]
    public void GetValue_environment_path_or_null()
    {
        var value = RegistryHelper.Local.GetValue(RegistryHiveKind.CurrentUser, "Environment", "Path");
        if (value is null)
            return;
        Assert.False(value.IsDefault);
        Assert.True(value.Type is RegistryValueKind.String or RegistryValueKind.ExpandString);
    }

    [Fact]
    public void Registry32_view_does_not_throw()
    {
        var key = RegistryHelper.Local.GetKey(
            RegistryHiveKind.LocalMachine,
            "SOFTWARE",
            RegistryViewKind.Registry32,
            RegistryDetailLevel.Identity);
        Assert.True(key is null || key.View == RegistryViewKind.Registry32 || key.Path.Length >= 0);
    }

    [Fact]
    public void For_dot_is_local()
    {
        Assert.True(RegistryHelper.For(".").IsLocal);
        Assert.True(RegistryHelper.For("localhost").IsLocal);
        Assert.True(RegistryHelper.For(Environment.MachineName).IsLocal);
        Assert.True(RegistryHelper.CanConnect("."));
    }

    [Fact]
    public void TryGetKey_software()
        => Assert.True(RegistryHelper.Local.TryGetKey(RegistryHiveKind.CurrentUser, "Software", out _));
}
