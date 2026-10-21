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
        return Token.Replace(template, match => Value(match.Groups[1].Value, number, letter, clock));
    }

    private static string Value(string body, long number, long letter, DateTime when)
    {
        var known = body.ToLowerInvariant() switch
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
            "public" => Folder(Environment.SpecialFolder.CommonDocuments) is var docs && docs.Length > 0
                ? Path.GetFullPath(Path.Combine(docs, ".."))
                : "",
            "username" => Environment.UserName,
            "computername" => Environment.MachineName,
            _ => null
        };
        return known ?? Counter(body, number, letter) ?? "{" + body + "}";
    }

    private static string Folder(Environment.SpecialFolder folder)
        => Environment.GetFolderPath(folder);

    private static string? Counter(string body, long number, long letter)
    {
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
        if (i != body.Length || (letters == 0 && digits == 0))
            return null;

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
