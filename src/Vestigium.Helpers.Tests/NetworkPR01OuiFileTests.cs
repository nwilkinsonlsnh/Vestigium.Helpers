using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPr01OuiFileTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "vest-pr01-oui-" + Guid.NewGuid().ToString("N"));

    public NetworkPr01OuiFileTests()
    {
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void PR01_008_huge_oui_file_rejected()
    {
        var path = Path.Combine(_dir, "huge.oui");
        using (var fs = File.Create(path))
            fs.SetLength(OuiRegistry.MaxFileBytes + 1);

        var ex = Assert.Throws<ArgumentException>(() => NetworkHelper.LoadOuiRegistry(path));
        Assert.Contains("bytes", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PR01_008_too_many_rows_rejected()
    {
        Assert.Throws<ArgumentException>(() => OuiRegistry.GuardRow(OuiRegistry.MaxRows + 1));
        OuiRegistry.GuardRow(OuiRegistry.MaxRows);
        OuiRegistry.GuardFile(new FileInfo(WriteSmall("ok.oui", "00:11:22,Acme\n")));
        var map = NetworkHelper.LoadOuiRegistry(Path.Combine(_dir, "ok.oui"));
        Assert.Equal("Acme", map["00:11:22"]);
    }

    private string WriteSmall(string name, string text)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, text);
        return path;
    }
}
