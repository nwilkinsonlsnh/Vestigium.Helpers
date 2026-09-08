using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Csv;

/// <summary>
/// RFC 4180 dialect plus the caller-set delimiter. Quote is locked to <c>"</c>.
/// Write always uses CRLF. Null options mean <see cref="Rfc4180"/>.
/// </summary>
public sealed record CsvOptions
{
    public const char Quote = '"';

    private char _delimiter = ',';

    /// <summary>Field separator. Default comma. Tab, semicolon, and pipe have presets.</summary>
    public char Delimiter
    {
        get => _delimiter;
        init => _delimiter = RequireLegal(value);
    }

    public bool HasHeaderRow { get; init; } = true;

    /// <summary>Write a UTF-8 BOM so Excel on Windows picks Unicode. Default on.</summary>
    public bool Utf8Bom { get; init; } = true;

    public bool NeutralizeInjection { get; init; } = true;

    /// <summary>Read only. Write never trims.</summary>
    public bool TrimFields { get; init; }

    public static CsvOptions Rfc4180 { get; } = new();

    public static CsvOptions Tab { get; } = new() { Delimiter = '\t' };

    public static CsvOptions Semicolon { get; } = new() { Delimiter = ';' };

    public static CsvOptions Pipe { get; } = new() { Delimiter = '|' };

    public static CsvOptions WithDelimiter(char delimiter) => new() { Delimiter = delimiter };

    public CsvOptions WithDelimiterChar(char delimiter) => new()
    {
        Delimiter = delimiter,
        HasHeaderRow = HasHeaderRow,
        Utf8Bom = Utf8Bom,
        NeutralizeInjection = NeutralizeInjection,
        TrimFields = TrimFields
    };

    public static CsvOptions Resolve(CsvOptions? options)
    {
        var resolved = options ?? Rfc4180;
        RequireLegal(resolved.Delimiter);
        return resolved;
    }

    public static char RequireLegal(char delimiter)
    {
        if (delimiter is Quote or '\r' or '\n' or '\0' || char.IsSurrogate(delimiter))
        {
            HelperLog.Error(
                HelperLog.CurrentAppId,
                VestigiumStatus.Failed,
                HelperLog.Subcategories.Session,
                $"Illegal CSV delimiter U+{((int)delimiter):X4}");
            throw new ArgumentOutOfRangeException(
                nameof(delimiter),
                delimiter,
                "Delimiter cannot be quote, CR, LF, NUL, or a surrogate.");
        }

        return delimiter;
    }

    public string DescribeDelimiter() => Delimiter switch
    {
        ',' => "comma",
        '\t' => "tab",
        ';' => "semicolon",
        '|' => "pipe",
        _ => $"U+{((int)Delimiter):X4}"
    };
}
