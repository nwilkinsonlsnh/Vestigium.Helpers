using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90RegFileTests : IDisposable
{
    private readonly string _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");

    public Branch90RegFileTests()
        => RegistryHelper.Local.CreateKey(RegistryHiveKind.CurrentUser, _root, confirm: true);

    public void Dispose()
        => RegistryHelper.Local.DeleteKey(RegistryHiveKind.CurrentUser, _root, recursive: true, confirm: true);

    [Fact]
    public void Hive_names_and_parse_tokens()
    {
        Assert.Equal("HKEY_CLASSES_ROOT", RegistryRegFile.HiveName(RegistryHiveKind.ClassesRoot));
        Assert.Equal("HKEY_CURRENT_USER", RegistryRegFile.HiveName(RegistryHiveKind.CurrentUser));
        Assert.Equal("HKEY_LOCAL_MACHINE", RegistryRegFile.HiveName(RegistryHiveKind.LocalMachine));
        Assert.Equal("HKEY_USERS", RegistryRegFile.HiveName(RegistryHiveKind.Users));
        Assert.Equal("HKEY_CURRENT_CONFIG", RegistryRegFile.HiveName(RegistryHiveKind.CurrentConfig));
        Assert.Throws<ArgumentOutOfRangeException>(() => RegistryRegFile.HiveName((RegistryHiveKind)99));

        Assert.True(RegistryRegFile.TryParseHive("HKCU", out var hive) && hive == RegistryHiveKind.CurrentUser);
        Assert.True(RegistryRegFile.TryParseHive("HKLM", out hive) && hive == RegistryHiveKind.LocalMachine);
        Assert.True(RegistryRegFile.TryParseHive("HKCR", out hive) && hive == RegistryHiveKind.ClassesRoot);
        Assert.True(RegistryRegFile.TryParseHive("HKU", out hive) && hive == RegistryHiveKind.Users);
        Assert.True(RegistryRegFile.TryParseHive("HKCC", out hive) && hive == RegistryHiveKind.CurrentConfig);
        Assert.False(RegistryRegFile.TryParseHive("NOPE", out _));
    }

    [Fact]
    public void Parse_headers_values_continuations_encodings()
    {
        Assert.True(RegistryRegFile.TryParseKeyHeader("[HKEY_CURRENT_USER\\Software\\X]", out var hive, out var path, out var del));
        Assert.Equal(RegistryHiveKind.CurrentUser, hive);
        Assert.Equal("Software\\X", path);
        Assert.False(del);
        Assert.True(RegistryRegFile.TryParseKeyHeader("[-HKEY_LOCAL_MACHINE]", out hive, out path, out del));
        Assert.True(del);
        Assert.Equal(string.Empty, path);
        Assert.False(RegistryRegFile.TryParseKeyHeader("not-a-key", out _, out _, out _));
        Assert.False(RegistryRegFile.TryParseKeyHeader("[BAD]", out _, out _, out _));

        Assert.True(RegistryRegFile.TryParseValue("@=\"hi\"", out var name, out del, out var kind, out var data, out _));
        Assert.Equal(string.Empty, name);
        Assert.Equal("hi", data);
        Assert.True(RegistryRegFile.TryParseValue("\"A\"=-", out name, out del, out _, out _, out _));
        Assert.True(del);
        Assert.True(RegistryRegFile.TryParseValue("\"D\"=dword:0000000a", out _, out _, out kind, out data, out _));
        Assert.Equal(RegistryValueKind.DWord, kind);
        Assert.True(RegistryRegFile.TryParseValue("\"Q\"=hex(b):01,00,00,00,00,00,00,00", out _, out _, out kind, out _, out _));
        Assert.Equal(RegistryValueKind.QWord, kind);
        Assert.True(RegistryRegFile.TryParseValue("\"B\"=hex:01,02", out _, out _, out kind, out _, out _));
        Assert.Equal(RegistryValueKind.Binary, kind);
        Assert.True(RegistryRegFile.TryParseValue("\"E\"=hex(2):00,00", out _, out _, out kind, out _, out _));
        Assert.Equal(RegistryValueKind.ExpandString, kind);
        Assert.True(RegistryRegFile.TryParseValue("\"M\"=hex(7):00,00", out _, out _, out kind, out _, out _));
        Assert.Equal(RegistryValueKind.MultiString, kind);
        Assert.True(RegistryRegFile.TryParseValue("\"N\"=hex(0):", out _, out _, out kind, out _, out _));
        Assert.Equal(RegistryValueKind.None, kind);
        Assert.False(RegistryRegFile.TryParseValue("; comment", out _, out _, out _, out _, out _));
        Assert.False(RegistryRegFile.TryParseValue("noequals", out _, out _, out _, out _, out _));
        Assert.False(RegistryRegFile.TryParseValue("@bogus", out _, out _, out _, out _, out _));
        Assert.False(RegistryRegFile.TryParseValue("\"open", out _, out _, out _, out _, out _));
        Assert.False(RegistryRegFile.TryParseValue("\"A\" hex", out _, out _, out _, out _, out _));
        Assert.False(RegistryRegFile.TryParseValue("\"A\"=weird:", out _, out _, out _, out _, out _));

        var lines = RegistryRegFile.PhysicalLines("a\\\r\n b\r\nc");
        Assert.Contains(lines, l => l.Contains('a'));
        Assert.Contains(lines, l => l == "c" || l.Contains('c'));

        var dir = Path.Combine(Path.GetTempPath(), "vest-regfile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var ansi = Path.Combine(dir, "a.reg");
            File.WriteAllText(ansi, "Windows Registry Editor Version 5.00\r\n");
            Assert.Contains("Registry", RegistryRegFile.ReadAllText(ansi));
            var utf16 = Path.Combine(dir, "u.reg");
            File.WriteAllBytes(utf16, [0xFF, 0xFE, .. System.Text.Encoding.Unicode.GetBytes("hi")]);
            Assert.Equal("hi", RegistryRegFile.ReadAllText(utf16));
            var utf8 = Path.Combine(dir, "8.reg");
            File.WriteAllBytes(utf8, [0xEF, 0xBB, 0xBF, .. System.Text.Encoding.UTF8.GetBytes("yo")]);
            Assert.Equal("yo", RegistryRegFile.ReadAllText(utf8));
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void Format_all_value_kinds_and_export_tree()
    {
        var hive = RegistryHiveKind.CurrentUser;
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(hive, _root, "S", "text", RegistryValueKind.String, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(hive, _root, "D", 7, RegistryValueKind.DWord, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(hive, _root, "Q", 8L, RegistryValueKind.QWord, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, RegistryHelper.Local.SetValue(hive, _root, "B", new byte[] { 1, 2 }, RegistryValueKind.Binary, confirm: true).Status);
        _ = RegistryHelper.Local.SetValue(hive, _root, string.Empty, "def", confirm: true);

        var snap = RegistryHelper.Local.GetKey(hive, _root, RegistryViewKind.Default, RegistryDetailLevel.Full);
        Assert.NotNull(snap);
        foreach (var value in snap!.Values)
            _ = RegistryRegFile.FormatValue(value);

        _ = RegistryRegFile.FormatValue(new RegistryValueInfo("E", RegistryValueKind.ExpandString, "%TEMP%", "%TEMP%", false));
        _ = RegistryRegFile.FormatValue(new RegistryValueInfo("M", RegistryValueKind.MultiString, new[] { "a", "b" }, "a b", false));
        _ = RegistryRegFile.FormatValue(new RegistryValueInfo("N", RegistryValueKind.None, Array.Empty<byte>(), "", false));
        _ = RegistryRegFile.FormatValue(new RegistryValueInfo("", RegistryValueKind.String, "x", "x", true));

        using var writer = new StringWriter();
        RegistryRegFile.WriteTree(RegistryHelper.Local, hive, _root, RegistryViewKind.Default, writer);
        Assert.Contains("Windows Registry Editor", writer.ToString());
        RegistryRegFile.WriteTree(RegistryHelper.Local, hive, _root + "\\missing", RegistryViewKind.Default, writer);

        _ = RegistryHelper.Search(hive, _root, "S", RegistrySearchMode.StartsWith);
        _ = RegistryHelper.Search(hive, _root, "ext", RegistrySearchMode.EndsWith);
        _ = RegistryHelper.Search(hive, _root, "te", RegistrySearchMode.Contains);
        _ = RegistryHelper.Local.DeleteValue(hive, _root, "S", confirm: true);
    }
}
