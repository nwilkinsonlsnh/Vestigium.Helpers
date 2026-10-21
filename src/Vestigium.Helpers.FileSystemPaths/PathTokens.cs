using System.Globalization;
using System.Text.RegularExpressions;

namespace Vestigium.Helpers.FileSystemPaths;

public static class PathTokens
{
    private static readonly Regex Token = new(@"\{([^{}]+)\}", RegexOptions.Compiled);

    public static string Expand(string? template, long number = 0, long letter = 0, DateTime? when = null)
    {
        if (string.IsNullOrEmpty(template))
            return "";
        var clock = when ?? DateTime.Now;
        var escaped = template.Replace("{{", "\u0001").Replace("}}", "\u0002");
        var expanded = Token.Replace(escaped, match => Value(match.Groups[1].Value, number, letter, clock));
        return expanded.Replace("\u0001", "{").Replace("\u0002", "}");
    }

    public static bool IsKnown(string body)
        => Named(body) || IsCounter(body);

    private static string Value(string body, long number, long letter, DateTime when)
        => Named(body) ? Resolve(body.ToLowerInvariant(), when) : Counter(body, number, letter) ?? "{" + body + "}";

    private static bool Named(string body)
        => body.ToLowerInvariant() switch
        {
            "datetime" => "datetime",
            "date" => "date",
            "time" => "time",
            "desktop" => "desktop",
            "favorites" => "favorites",
            "userprofile" => "userprofile",
            "appdata" => "appdata",
            "localappdata" => "localappdata",
            "temp" => "temp",
            "programfiles" => "programfiles",
            "programfilesx86" => "programfilesx86",
            "programdata" => "programdata",
            "windir" => "windir",
            "systemdrive" => "systemdrive",
            "public" => "public",
            "username" => "username",
            "computername" => "computername",
            _ => false
        };

    private static string Resolve(string name, DateTime when)
    {
        return name switch
        {
            "datetime" => when.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture),
            "date" => when.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            "time" => when.ToString("HHmmss", CultureInfo.InvariantCulture),
            "desktop" => Folder(Environment.SpecialFolder.Desktop),
            "favorites" => Folder(Environment.SpecialFolder.Favorites),
            "userprofile" => Folder(Environment.SpecialFolder.UserProfile),
            "appdata" => Folder(Environment.SpecialFolder.ApplicationData),
            "localappdata" => Folder(Environment.SpecialFolder.LocalApplicationData),
            "temp" => Path.GetTempPath().TrimEnd('\\'),
            "programfiles" => Folder(Environment.SpecialFolder.ProgramFiles),
            "programfilesx86" => Folder(Environment.SpecialFolder.ProgramFilesX86),
            "programdata" => Folder(Environment.SpecialFolder.CommonApplicationData),
            "windir" => Folder(Environment.SpecialFolder.Windows),
            "systemdrive" => Path.GetPathRoot(Folder(Environment.SpecialFolder.Windows))?.TrimEnd('\\') ?? "",
            "public" => PublicFolder(),
            "username" => Environment.UserName,
            "computername" => Environment.MachineName,
            _ => ""
        };
    }

    private static string Folder(Environment.SpecialFolder folder)
        => Environment.GetFolderPath(folder);

    private static string PublicFolder()
    {
        var docs = Folder(Environment.SpecialFolder.CommonDocuments);
        return docs.Length == 0 ? "" : Path.GetFullPath(Path.Combine(docs, ".."));
    }

    private static bool IsCounter(string body)
    {
        var i = 0;
        if (body.Length > 0 && body[0] is 'A' or 'a')
        {
            var mark = body[0];
            while (i < body.Length && body[i] == mark)
                i++;
        }
        var digits = 0;
        while (i < body.Length && body[i] == '#')
        {
            digits++;
            i++;
        }
        return i == body.Length && (i > 0);
    }

    private static string? Counter(string body, long number, long letter)
    {
        if (!IsCounter(body))
            return null;
        var letters = 0;
        var upper = true;
        var i = 0;
        if (body.Length > 0 && body[0] is 'A' or 'a')
        {
            upper = body[0] == 'A';
            while (i < body.Length && body[i] == body[0])
            {
                letters++;
                i++;
            }
        }
        var digits = 0;
        while (i < body.Length && body[i] == '#')
        {
            digits++;
            i++;
        }
        var letterText = letters == 0 ? "" : Letters(letter, letters, upper);
        var numberText = digits == 0 ? "" : Digits(number, digits);
        return letterText + numberText;
    }

    private static string Digits(long value, int width)
    {
        var max = 1L;
        for (var i = 0; i < width; i++)
            max *= 10;
        if (value < 0 || value >= max)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Counter does not fit {" + new string('#', width) + "}.");
        return value.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
    }

    private static string Letters(long value, int width, bool upper)
    {
        var max = 1L;
        for (var i = 0; i < width; i++)
            max *= 26;
        if (value < 0 || value >= max)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Letter counter does not fit.");
        var chars = new char[width];
        var baseChar = upper ? 'A' : 'a';
        for (var i = width - 1; i >= 0; i--)
        {
            chars[i] = (char)(baseChar + (value % 26));
            value /= 26;
        }
        return new string(chars);
    }
}
