using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryBacklogB1Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;

    public RegistryBacklogB1Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.CreateKey(Hive, _root, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "", "default-row", confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "Mark", "named", confirm: true).Status);
    }

    public void Dispose()
        => RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);

    [Fact]
    public void Full_get_key_has_last_write_and_default_value()
    {
        var key = RegistryHelper.Local.GetKey(Hive, _root, level: RegistryDetailLevel.Full);
        Assert.NotNull(key);
        Assert.NotNull(key!.LastWriteTime);
        Assert.True(key.LastWriteTime <= DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.Contains(key.Values, v => v.IsDefault && v.DataText == "default-row");
        Assert.Contains(key.Values, v => v.Name == "Mark");
    }

    [Fact]
    public void Slim_get_key_skips_values_but_may_omit_last_write()
    {
        var key = RegistryHelper.Local.GetKey(Hive, _root, level: RegistryDetailLevel.Slim);
        Assert.NotNull(key);
        Assert.Empty(key!.Values);
        Assert.Null(key.LastWriteTime);
    }
}
