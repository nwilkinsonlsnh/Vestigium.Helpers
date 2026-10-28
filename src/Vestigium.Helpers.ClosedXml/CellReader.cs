using ClosedXML.Excel;

namespace Vestigium.Helpers.ClosedXml;

/// <summary>
/// Typed guess for cells this library wrote. Not a general Excel importer.
/// </summary>
internal static class CellReader
{
    public static object? Read(IXLCell cell)
    {
        var value = cell.Value;

        return value switch
        {
            { IsBlank: true } => null,
            { IsBoolean: true } => value.GetBoolean( ),
            { IsDateTime: true } => value.GetDateTime( ),
            { IsTimeSpan: true } => value.GetTimeSpan( ).TotalDays,

            // Use a 'when' clause to evaluate the extra conditions for numbers
            { IsNumber: true } when LooksLikeDateFormat(cell) && cell.TryGetValue(out DateTime dt) => dt,
            { IsNumber: true } => value.GetNumber( ),

            { IsText: true } => value.GetText( ),
            _ => cell.GetString( )
        };
    }

    public static string KindOf(object? value) => value switch
    {
        null => "blank",
        bool => "bool",
        DateTime => "datetime",
        string => "text",
        _ => "number"
    };

    private static bool LooksLikeDateFormat(IXLCell cell)
    {
        var fmt = cell.Style.DateFormat.Format;
        if (string.IsNullOrWhiteSpace(fmt))
            fmt = cell.Style.NumberFormat.Format;
        if (string.IsNullOrWhiteSpace(fmt))
            return false;
        var f = fmt.ToLowerInvariant();
        if (f.Contains("[h]", StringComparison.Ordinal))
            return false;
        return f.Contains('y') || f.Contains("dd") || (f.Contains('d') && f.Contains('m'));
    }
}
