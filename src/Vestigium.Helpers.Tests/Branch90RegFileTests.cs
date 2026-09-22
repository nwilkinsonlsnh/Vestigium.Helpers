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
        Assert.False(del);
        Assert.True(RegistryRegFile.TryParseKeyHeader("[-HKEY_LOCAL_MACHINE]", out hive, out path, out del));
        Assert.True(del);
        Assert.False(RegistryRegFile.TryParseKeyHeader("not-a-key", out _, out _, out _));
        Assert.False(RegistryRegFile.TryParseKeyHeader("[BAD]", out _, out _, out _));

        Assert.True(RegistryRegFile.TryParseValue("@=\"hi\"", out var name, out del, out var kind, out var data, out _));
        Assert.Equal(string.Empty, name);
        Assert.True(RegistryRegFile.TryParseValue("\"A\"=-", out name, out del, out _, out _, out _));
        Assert.True(del);
        Assert.True(RegistryRegFile.TryParseValue("\"D\"=dword:0000000a", out _, out _, out kind, out _, out _));
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

        _ = RegistryRegFile.PhysicalLines("a\\\r\n b\r\nc");

        var dir = Path.Combine(Path.GetTempPath(), "vest-regfile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "a.reg"), "Windows Registry Editor Version 5.00\r\n");
            Assert.Contains("Registry", RegistryRegFile.ReadAllText(Path.Combine(dir, "a.reg")));
            File.WriteAllBytes(Path.Combine(dir, "u.reg"), [0xFF, 0xFE, .. System.Text.Encoding.Unicode.GetBytes("hi")]);
            Assert.Equal("hi", RegistryRegFile.ReadAllText(Path.Combine(dir, "u.reg")));
            File.WriteAllBytes(Path.Combine(dir, "8.reg"), [0xEF, 0xBB, 0xBF, .. System.Text.Encoding.UTF8.GetBytes("yo")]);
            Assert.Equal("yo", RegistryRegFile.ReadAllText(Path.Combine(dir, "8.reg")));
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void Format_all_value_kinds_and_client_write_edges()
    {
        var hive = RegistryHiveKind.CurrentUser;
        var client = RegistryHelper.Local;
        Assert.Equal(RegistryWriteStatus.Denied, client.CreateKey(hive, _root + "\\x", confirm: false).Status);
        Assert.Equal(RegistryWriteStatus.InvalidPath, client.CreateKey(hive, "", confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Denied, client.SetValue(hive, _root, "S", "text", confirm: false).Status);
        Assert.Equal(RegistryWriteStatus.TypeMismatch, client.SetValue(hive, _root, "S", 1, RegistryValueKind.String, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Denied, client.SetValue(RegistryHiveKind.LocalMachine, "SYSTEM", "x", "y", confirm: true).Status);

        Assert.Equal(RegistryWriteStatus.Ok, client.SetValue(hive, _root, "S", "text", RegistryValueKind.String, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, client.SetValue(hive, _root, "D", 7, RegistryValueKind.DWord, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, client.SetValue(hive, _root, "Q", 8L, RegistryValueKind.QWord, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, client.SetValue(hive, _root, "B", new byte[] { 1, 2 }, RegistryValueKind.Binary, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, client.SetValue(hive, _root, "E", "%TEMP%", RegistryValueKind.ExpandString, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, client.SetValue(hive, _root, "M", new[] { "a", "b" }, RegistryValueKind.MultiString, confirm: true).Status);

        var snap = client.GetKey(hive, _root, RegistryViewKind.Default, RegistryDetailLevel.Full);
        Assert.NotNull(snap);
        foreach (var value in snap!.Values)
            _ = RegistryRegFile.FormatValue(value);

        _ = RegistryRegFile.FormatValue(new RegistryValueInfo { Name = "E", Type = RegistryValueKind.ExpandString, Data = "%TEMP%", DataText = "%TEMP%" });
        _ = RegistryRegFile.FormatValue(new RegistryValueInfo { Name = "M", Type = RegistryValueKind.MultiString, Data = new[] { "a", "b" } });
        _ = RegistryRegFile.FormatValue(new RegistryValueInfo { Name = "N", Type = RegistryValueKind.None, Data = Array.Empty<byte>() });
        _ = RegistryRegFile.FormatValue(new RegistryValueInfo { Name = "", IsDefault = true, Type = RegistryValueKind.String, Data = "x" });
        _ = RegistryRegFile.FormatValue(new RegistryValueInfo { Name = "U", Type = RegistryValueKind.Unknown, DataText = "z" });

        using var writer = new StringWriter();
        RegistryRegFile.WriteTree(client, hive, _root, RegistryViewKind.Default, writer);
        Assert.Contains("Windows Registry Editor", writer.ToString());
        RegistryRegFile.WriteTree(client, hive, _root + "\\missing", RegistryViewKind.Default, writer);

        _ = RegistryHelper.Search(hive, _root, "S", RegistrySearchMode.StartsWith);
        _ = RegistryHelper.Search(hive, _root, "ext", RegistrySearchMode.EndsWith);
        _ = RegistryHelper.Search(hive, _root, "te", RegistrySearchMode.Contains);
        Assert.Equal(RegistryWriteStatus.Ok, client.DeleteValue(hive, _root, "S", confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Denied, client.DeleteValue(hive, _root, "D", confirm: false).Status);
        Assert.Equal(RegistryWriteStatus.Denied, client.DeleteKey(hive, _root, confirm: false).Status);
    }
}
