namespace Vestigium.Helpers.ClosedXml;

/// <summary>
/// Rectangular table dumped onto a worksheet. Callers pass this instead of a DataTable.
/// </summary>
public sealed class SheetTable
{
    public required IReadOnlyList<string> Headers { get; init; }
    public required IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; }
    public string? Name { get; init; }

    public static SheetTable Create(
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<object?>> rows,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);
        if (headers.Count == 0)
            throw new ArgumentException("A table needs at least one header.", nameof(headers));

        return new SheetTable
        {
            Headers = headers,
            Rows = rows as IReadOnlyList<IReadOnlyList<object?>> ?? rows.ToArray(),
            Name = name
        };
    }

    public static SheetTable KeyValue(string keyHeader, string valueHeader, IEnumerable<(string Key, object? Value)> pairs, string? name = null)
        => Create(
            [keyHeader, valueHeader],
            pairs.Select(p => (IReadOnlyList<object?>)[p.Key, p.Value]),
            name);

    public static string CellKind(object? value) => CellReader.KindOf(value);
}

public sealed class SheetWriteOptions
{
    public bool HasHeaderRow { get; init; } = true;
    public bool CreateExcelTable { get; init; } = true;
    public bool Autosize { get; init; } = true;
    public double AutosizeMaxWidth { get; init; } = 40;
    public bool FreezeHeader { get; init; } = true;
    public bool AutoFilter { get; init; } = true;
    public string? NumberFormat { get; init; }
    public string? DateFormat { get; init; }
    /// <summary>Sheet tab color as <c>#RRGGBB</c> or <c>RRGGBB</c>.</summary>
    public string? TabColor { get; init; }
    /// <summary>
    /// Excel table style from the Table Design gallery.
    /// Examples: <c>Medium2</c> (default), <c>Light9</c>, <c>Dark7</c>, <c>TableStyleMedium9</c>, <c>None</c>.
    /// Null inherits <see cref="WorkbookSession.TableStyle"/>.
    /// </summary>
    public string? TableStyle { get; init; }
    /// <summary>Landscape, fit-to-width, footer with APPID and print date/time.</summary>
    public bool OperatorPrint { get; init; } = true;
    /// <summary>Number formats from header names: ms, pct, utc.</summary>
    public bool HeaderNumberFormats { get; init; } = true;
    /// <summary>Header of a numeric column to highlight when greater than <see cref="HighlightGreaterThan"/>.</summary>
    public string? HighlightColumn { get; init; }
    /// <summary>Threshold for the single high-outlier rule. Ignored when HighlightColumn is blank.</summary>
    public double? HighlightGreaterThan { get; init; }

    public static SheetWriteOptions Default { get; } = new();

    /// <summary>
    /// Fill a letterhead named range: do not wipe print setup, freeze, or wrap an Excel table
    /// around the operator's chrome.
    /// </summary>
    public static SheetWriteOptions Letterhead { get; } = new()
    {
        CreateExcelTable = false,
        Autosize = false,
        FreezeHeader = false,
        AutoFilter = false,
        OperatorPrint = false,
        HeaderNumberFormats = false
    };
}

public sealed class SheetReadOptions
{
    public bool HasHeaderRow { get; init; } = true;

    public static SheetReadOptions Default { get; } = new();
}

public sealed class SheetChrome
{
    public bool BoldHeader { get; init; } = true;
    public bool FreezeHeader { get; init; } = true;
    public bool AutoFilter { get; init; } = true;
    public string? TabColor { get; init; }
    public bool OperatorPrint { get; init; } = true;

    public static SheetChrome Default { get; } = new();
}
