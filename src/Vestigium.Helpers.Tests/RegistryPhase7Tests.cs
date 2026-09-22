using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryPhase7Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;

    public RegistryPhase7Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.CreateKey(Hive, _root + @"\Child", confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "MarkName", "alpha-secret-payload", confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "Bin", new byte[] { 1, 2, 3 }, RegistryValueKind.Binary, confirm: true).Status);
    }

    public void Dispose()
        => RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);

    [Fact]
    public void Search_contains_value_name()
    {
        var hits = RegistryHelper.Search(Hive, _root, "Mark", RegistrySearchMode.Contains, RegistrySearchFields.ValueName);
        Assert.Contains(hits, h => h.ValueName == "MarkName");
    }

    [Fact]
    public void Search_starts_with_key_name()
    {
        var hits = RegistryHelper.Search(Hive, _root, "Child", RegistrySearchMode.StartsWith, RegistrySearchFields.KeyName);
        Assert.Contains(hits, h => h.Path.EndsWith("Child", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Search_value_data_finds_string_not_binary()
    {
        var hits = RegistryHelper.Search(Hive, _root, "alpha-secret", RegistrySearchMode.Contains, RegistrySearchFields.ValueData);
        Assert.Contains(hits, h => h.ValueName == "MarkName");
        Assert.DoesNotContain(hits, h => h.ValueName == "Bin");
    }

    [Fact]
    public void Search_max_results_over_cap_throws()
        => Assert.Throws<ArgumentException>(() => RegistryHelper.Search(Hive, _root, "Mark", maxResults: 300));

    [Fact]
    public void Snapshots_have_no_password_property()
    {
        Assert.Null(typeof(RegistryKeyInfo).GetProperty("Password"));
        Assert.Null(typeof(RegistryValueInfo).GetProperty("Password"));
        Assert.Null(typeof(RegistryWriteResult).GetProperty("Password"));
        Assert.Null(typeof(RegistryHit).GetProperty("Password"));
    }
}
