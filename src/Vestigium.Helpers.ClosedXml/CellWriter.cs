using ClosedXML.Excel;
using Vestigium.Logging;

namespace Vestigium.Helpers.ClosedXml;

internal static class CellWriter
{
    public static bool Write(IXLCell cell, object? value, SheetWriteOptions options)
    {
        if (value is not null)
            return value switch
            {
                string s => WriteString(cell, s),
                bool b => WriteBool(cell, b),
                DateTime or DateTimeOffset or TimeSpan => WriteDate(cell, value, options),
                decimal or float or double or byte or sbyte or short or ushort or int or uint or long or ulong
                    => WriteNumber(cell, value, options),
                _ => WriteString(cell,
                    Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty)
            };
        cell.Clear();
        return false;

    }

    internal static bool WriteBool(IXLCell cell, bool value)
    {
        cell.Value = value;
        return false;
    }

    internal static bool WriteDate(IXLCell cell, object value, SheetWriteOptions options)
    {
        switch (value)
        {
            case DateTime dt:
                cell.Value = dt;
                ApplyDate(cell, options);
                return false;
            case DateTimeOffset dto:
                cell.Value = dto.UtcDateTime;
                ApplyDate(cell, options);
                return false;
            case TimeSpan ts:
                cell.Value = ts.TotalDays;
                cell.Style.NumberFormat.Format = string.IsNullOrWhiteSpace(options.DateFormat)
                    ? "[h]:mm:ss"
                    : options.DateFormat;
                return false;
            default:
                return WriteString(cell, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    internal static bool WriteNumber(IXLCell cell, object value, SheetWriteOptions options)
    {
        switch (value)
        {
            case decimal d:
                cell.Value = d;
                ApplyNumber(cell, options);
                return false;
            case float or double:
                var number = Convert.ToDouble(value);
                if (!double.IsFinite(number))
                {
                    ClosedXmlLog.Error(
                        ClosedXmlEvents.CellRejectedNonFinite,
                        ClosedXmlCatalog.Subcategories.Sheet,
                        "rejected non-finite number",
                        properties: CellProps(cell, ("reason", "non-finite")));
                    throw new ArgumentOutOfRangeException(nameof(value), "Value is not a finite number.");
                }
                cell.Value = number;
                ApplyNumber(cell, options);
                return false;
            default:
                return WriteInteger(cell, value, options);
        }
    }

    internal static bool WriteInteger(IXLCell cell, object value, SheetWriteOptions options)
    {
        if (value is ulong ul)
        {
            if (ul <= long.MaxValue)
                cell.Value = (long)ul;
            else
                cell.Value = (double)ul;
            ApplyNumber(cell, options);
            return false;
        }

        cell.Value = Convert.ToInt64(value);
        ApplyNumber(cell, options);
        return false;
    }

    private static bool WriteString(IXLCell cell, string text)
    {
        var neutralized = Neutralize(text);
        var stored = neutralized.Changed ? "'" + neutralized.Text : neutralized.Text;
        cell.SetValue(stored);
        if (neutralized.Changed)
        {
            ClosedXmlLog.Warning(
                ClosedXmlEvents.CellNeutralized,
                ClosedXmlCatalog.Subcategories.Sheet,
                "neutralized formula-like text",
                properties: CellProps(cell, ("lead", text.Length == 0 ? null : text[0].ToString())));
        }

        return neutralized.Changed;
    }

    private static IReadOnlyDictionary<string, string?> CellProps(IXLCell cell, params (string Key, string? Value)[] extra)
    {
        var pairs = new List<(string, string?)>
        {
            ("sheet", cell.Worksheet.Name),
            ("cell", cell.Address.ToString())
        };
        pairs.AddRange(extra);
        return ClosedXmlLog.Props(pairs.ToArray());
    }

    private static void ApplyNumber(IXLCell cell, SheetWriteOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.NumberFormat))
            cell.Style.NumberFormat.Format = options.NumberFormat;
    }

    private static void ApplyDate(IXLCell cell, SheetWriteOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.DateFormat))
            cell.Style.DateFormat.Format = options.DateFormat;
    }

    public static (string Text, bool Changed) Neutralize(string text)
    {
        if (text.Length == 0)
            return (text, false);
        var lead = text[0];
        return lead is not ('=' or '+' or '-' or '@') ? (text, false) : ("'" + text, true);
    }
}
