using System.Globalization;
using System.Text;

namespace Vestigium.Helpers.WinReg;

internal static class RegistryRegFile
{
    public const string Header50 = "Windows Registry Editor Version 5.00";
    public const string Header40 = "REGEDIT4";

    public static string HiveName(RegistryHiveKind hive) => hive switch
    {
        RegistryHiveKind.ClassesRoot => "HKEY_CLASSES_ROOT",
        RegistryHiveKind.CurrentUser => "HKEY_CURRENT_USER",
        RegistryHiveKind.LocalMachine => "HKEY_LOCAL_MACHINE",
        RegistryHiveKind.Users => "HKEY_USERS",
        RegistryHiveKind.CurrentConfig => "HKEY_CURRENT_CONFIG",
        _ => throw new ArgumentOutOfRangeException(nameof(hive))
    };

    public static bool TryParseHive(string token, out RegistryHiveKind hive)
    {
        hive = token.ToUpperInvariant() switch
        {
            "HKEY_CLASSES_ROOT" or "HKCR" => RegistryHiveKind.ClassesRoot,
            "HKEY_CURRENT_USER" or "HKCU" => RegistryHiveKind.CurrentUser,
            "HKEY_LOCAL_MACHINE" or "HKLM" => RegistryHiveKind.LocalMachine,
            "HKEY_USERS" or "HKU" => RegistryHiveKind.Users,
            "HKEY_CURRENT_CONFIG" or "HKCC" => RegistryHiveKind.CurrentConfig,
            _ => (RegistryHiveKind)(-1)
        };
        return (int)hive >= 0;
    }

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

    public static string ReadAllText(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        return Encoding.ASCII.GetString(bytes);
    }

    public static List<string> PhysicalLines(string text)
    {
        var raw = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var lines = new List<string>();
        var buffer = new StringBuilder();
        foreach (var line in raw)
        {
            var trimmed = line.TrimEnd();
            if (trimmed.EndsWith('\\') && !trimmed.StartsWith(';'))
            {
                buffer.Append(trimmed.TrimEnd('\\').TrimEnd());
                continue;
            }

            if (buffer.Length > 0)
            {
                buffer.Append(trimmed.TrimStart());
                lines.Add(buffer.ToString());
                buffer.Clear();
            }
            else
            {
                lines.Add(trimmed);
            }
        }

        if (buffer.Length > 0)
            lines.Add(buffer.ToString());
        return lines;
    }

    public static bool TryParseKeyHeader(string line, out RegistryHiveKind hive, out string path, out bool delete)
    {
        hive = default;
        path = string.Empty;
        delete = false;
        var text = line.Trim();
        if (text.Length < 3 || text[0] != '[' || text[^1] != ']')
            return false;
        text = text[1..^1];
        if (text.StartsWith('-'))
        {
            delete = true;
            text = text[1..];
        }

        var slash = text.IndexOf('\\');
        var hiveToken = slash < 0 ? text : text[..slash];
        path = slash < 0 ? string.Empty : text[(slash + 1)..];
        return TryParseHive(hiveToken, out hive);
    }

    public static bool TryParseValue(string line, out string name, out bool delete, out RegistryValueKind kind, out object? data, out string? error)
    {
        name = string.Empty;
        delete = false;
        kind = RegistryValueKind.String;
        data = null;
        error = null;
        var text = line.Trim();
        if (text.Length == 0 || text.StartsWith(';') || text.StartsWith('['))
            return false;

        string rawName;
        string rawValue;
        if (text.StartsWith('@'))
        {
            rawName = string.Empty;
            if (text.Length < 2 || text[1] != '=')
            {
                error = "default value missing =";
                return false;
            }
            rawValue = text[2..];
        }
        else if (text.StartsWith('"'))
        {
            var close = FindCloseQuote(text, 1);
            if (close < 0)
            {
                error = "unterminated value name";
                return false;
            }
            rawName = Unescape(text[1..close]);
            var rest = text[(close + 1)..].TrimStart();
            if (!rest.StartsWith('='))
            {
                error = "value missing =";
                return false;
            }
            rawValue = rest[1..];
        }
        else
        {
            error = "value line";
            return false;
        }

        name = rawName;
        rawValue = rawValue.Trim();
        if (rawValue == "-")
        {
            delete = true;
            return true;
        }

        if (rawValue.StartsWith('"'))
        {
            kind = RegistryValueKind.String;
            data = Unescape(TrimQuoted(rawValue));
            return true;
        }

        if (rawValue.StartsWith("dword:", StringComparison.OrdinalIgnoreCase))
        {
            kind = RegistryValueKind.DWord;
            var hex = rawValue[6..].Trim();
            data = unchecked((int)uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            return true;
        }

        if (rawValue.StartsWith("hex(b):", StringComparison.OrdinalIgnoreCase))
        {
            kind = RegistryValueKind.QWord;
            var bytes = ParseHex(rawValue[7..]);
            data = bytes.Length >= 8 ? BitConverter.ToInt64(bytes, 0) : 0L;
            return true;
        }

        if (rawValue.StartsWith("hex(2):", StringComparison.OrdinalIgnoreCase))
        {
            kind = RegistryValueKind.ExpandString;
            data = Encoding.Unicode.GetString(ParseHex(rawValue[7..])).TrimEnd('\0');
            return true;
        }

        if (rawValue.StartsWith("hex(7):", StringComparison.OrdinalIgnoreCase))
        {
            kind = RegistryValueKind.MultiString;
            data = Encoding.Unicode.GetString(ParseHex(rawValue[7..])).TrimEnd('\0').Split('\0', StringSplitOptions.RemoveEmptyEntries);
            return true;
        }

        if (rawValue.StartsWith("hex(0):", StringComparison.OrdinalIgnoreCase))
        {
            kind = RegistryValueKind.None;
            data = ParseHex(rawValue[7..]);
            return true;
        }

        if (rawValue.StartsWith("hex:", StringComparison.OrdinalIgnoreCase))
        {
            kind = RegistryValueKind.Binary;
            data = ParseHex(rawValue[4..]);
            return true;
        }

        error = "unknown value form";
        return false;
    }

    private static string TrimQuoted(string raw)
    {
        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
            return raw[1..^1];
        return raw;
    }

    private static int FindCloseQuote(string text, int start)
    {
        for (var i = start; i < text.Length; i++)
        {
            if (text[i] == '\\')
            {
                i++;
                continue;
            }
            if (text[i] == '"')
                return i;
        }
        return -1;
    }

    private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string Unescape(string text) => text.Replace("\\\"", "\"").Replace("\\\\", "\\");

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

    private static byte[] ParseHex(string text)
    {
        var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var bytes = new byte[parts.Length];
        for (var i = 0; i < parts.Length; i++)
            bytes[i] = byte.Parse(parts[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return bytes;
    }
}
