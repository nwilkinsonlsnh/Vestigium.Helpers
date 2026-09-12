using System.Globalization;
using System.Text;

namespace Vestigium.Helpers.WinReg;

internal static class RegistryRegFile
{
    public const string Header50 = "Windows Registry Editor Version 5.00";

    public static string HiveName(RegistryHiveKind hive) => hive switch
    {
        RegistryHiveKind.ClassesRoot => "HKEY_CLASSES_ROOT",
        RegistryHiveKind.CurrentUser => "HKEY_CURRENT_USER",
        RegistryHiveKind.LocalMachine => "HKEY_LOCAL_MACHINE",
        RegistryHiveKind.Users => "HKEY_USERS",
        RegistryHiveKind.CurrentConfig => "HKEY_CURRENT_CONFIG",
        _ => throw new ArgumentOutOfRangeException(nameof(hive))
    };

    public static void WriteTree(RegistryClient client, RegistryHiveKind hive, string path, RegistryViewKind view, TextWriter writer)
    {
        writer.WriteLine(Header50);
        writer.WriteLine();
        WriteKey(client, hive, path, view, writer);
    }

    private static void WriteKey(RegistryClient client, RegistryHiveKind hive, string path, RegistryViewKind view, TextWriter writer)
    {
        var snap = client.GetKey(hive, path, view, RegistryDetailLevel.Full);
        if (snap is null)
            return;

        var full = string.IsNullOrEmpty(path) ? HiveName(hive) : HiveName(hive) + "\\" + path;
        writer.WriteLine("[" + full + "]");
        foreach (var value in snap.Values)
            writer.WriteLine(FormatValue(value));
        writer.WriteLine();

        foreach (var child in snap.SubKeyNames)
        {
            var next = string.IsNullOrEmpty(path) ? child : path + "\\" + child;
            WriteKey(client, hive, next, view, writer);
        }
    }

    public static string FormatValue(RegistryValueInfo value)
    {
        var name = value.IsDefault || value.Name.Length == 0 ? "@" : "\"" + Escape(value.Name) + "\"";
        return value.Type switch
        {
            RegistryValueKind.String => name + "=\"" + Escape(value.Data as string ?? string.Empty) + "\"",
            RegistryValueKind.DWord => name + "=dword:" + ToDword(value.Data),
            RegistryValueKind.QWord => name + "=hex(b):" + ToHex(QwordBytes(value.Data)),
            RegistryValueKind.Binary => name + "=hex:" + ToHex(value.Data as byte[] ?? []),
            RegistryValueKind.ExpandString => name + "=hex(2):" + ToHex(Encoding.Unicode.GetBytes((value.Data as string ?? string.Empty) + "\0")),
            RegistryValueKind.MultiString => name + "=hex(7):" + ToHex(MultiBytes(value.Data as string[] ?? [])),
            RegistryValueKind.None => name + "=hex(0):",
            _ => name + "=\"" + Escape(value.DataText ?? string.Empty) + "\""
        };
    }

    private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string ToDword(object? data)
    {
        var number = data is int i ? unchecked((uint)i) : Convert.ToUInt32(data, CultureInfo.InvariantCulture);
        return number.ToString("x8", CultureInfo.InvariantCulture);
    }

    private static byte[] QwordBytes(object? data)
    {
        var number = data is long l ? l : Convert.ToInt64(data, CultureInfo.InvariantCulture);
        return BitConverter.GetBytes(number);
    }

    private static byte[] MultiBytes(string[] parts)
    {
        var buffer = new List<byte>();
        foreach (var part in parts)
        {
            buffer.AddRange(Encoding.Unicode.GetBytes(part));
            buffer.Add(0);
            buffer.Add(0);
        }
        buffer.Add(0);
        buffer.Add(0);
        return [.. buffer];
    }

    private static string ToHex(byte[] bytes) => string.Join(",", bytes.Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
}
