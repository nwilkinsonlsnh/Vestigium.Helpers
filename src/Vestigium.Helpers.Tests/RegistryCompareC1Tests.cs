using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryCompareC1Tests : IDisposable
{
    private static readonly RegistryHiveKind Hive = RegistryHiveKind.CurrentUser;
    private readonly string _root;
    private readonly string _dir;

    public RegistryCompareC1Tests()
    {
        _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
        _dir = Path.Combine(Path.GetTempPath(), "vest-regidx-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.CreateKey(Hive, _root + @"\Child", confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(Hive, _root, "Mark", "idx-secret", confirm: true).Status);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(Hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void WriteIndex_without_confirm_does_not_create_file()
    {
        var file = Path.Combine(_dir, "no.jsonl");
        var result = RegistryHelper.WriteIndex(file, Hive, _root, confirm: false);
        Assert.Equal(RegistryWriteStatus.Denied, result.Status);
        Assert.False(File.Exists(file));
    }

    [Fact]
    public void WriteIndex_missing_key_is_not_found()
    {
        var result = RegistryHelper.WriteIndex(Path.Combine(_dir, "miss.jsonl"), Hive, _root + @"\gone", confirm: true);
        Assert.Equal(RegistryWriteStatus.NotFound, result.Status);
    }

    [Fact]
    public void WriteIndex_writes_schema_keys_and_hashes_not_payload()
    {
        var file = Path.Combine(_dir, "idx.jsonl");
        var ticks = new List<RegistryCompareProgress>();
        var result = RegistryHelper.WriteIndex(file, Hive, _root, confirm: true, progress: new Progress<RegistryCompareProgress>(ticks.Add));
        Assert.Equal(RegistryWriteStatus.Ok, result.Status);
        var text = File.ReadAllText(file);
        Assert.Contains("vest-regidx/1", text);
        Assert.Contains("\"rec\":\"header\"", text);
        Assert.Contains("\"rec\":\"key\"", text);
        Assert.Contains("\"rec\":\"value\"", text);
        Assert.Contains("\"rec\":\"footer\"", text);
        Assert.Contains("Mark", text);
        Assert.DoesNotContain("idx-secret", text);
        Assert.Contains("Child", text);
    }
}
