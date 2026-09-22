using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryAclA5Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;

    public RegistryAclA5Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        RegistryHelper.Local.CreateKey(Hive, _root, confirm: true);
    }

    public void Dispose()
        => RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);

    [Fact]
    public void Slim_skips_acl()
    {
        var slim = RegistryHelper.Local.GetKey(Hive, _root, level: RegistryDetailLevel.Slim);
        Assert.NotNull(slim);
        Assert.Null(slim!.Owner);
        Assert.Null(slim.Sddl);
    }

    [Fact]
    public void Full_has_owner_and_sddl()
    {
        var full = RegistryHelper.Local.GetKey(Hive, _root, level: RegistryDetailLevel.Full);
        Assert.NotNull(full);
        Assert.False(string.IsNullOrWhiteSpace(full!.Owner));
        Assert.False(string.IsNullOrWhiteSpace(full.Sddl));
        Assert.Contains("O:", full.Sddl, StringComparison.OrdinalIgnoreCase);
    }
}
