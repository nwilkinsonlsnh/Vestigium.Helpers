using ClosedXML.Excel;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.ClosedXml;

internal static class CellWriter
{
    public static bool Write(IXLCell cell, object? value, SheetWriteOptions options)
    {
        if (value is null)
        {
            cell.Clear();
            return false;
        }

        return value switch
        {
            string s => WriteString(cell, s),
            bool b => WriteBool(cell, b),
            DateTime or DateTimeOffset or TimeSpan => WriteDate(cell, value, options),
            decimal or float or double or byte or sbyte or short or ushort or int or uint or long or ulong
                => WriteNumber(cell, value, options),
            _ => WriteString(cell, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty)
        };
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
                HelperGuard.Finite(number, nameof(value));
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
        // ClosedXML treats a single leading apostrophe as Excel's quote-prefix
        // and strips it from the stored text. Prefix once more so GetString()
        // still starts with "'" after save/reopen.
        var stored = neutralized.Changed ? "'" + neutralized.Text : neutralized.Text;
        cell.SetValue(stored);
        if (neutralized.Changed)
        {
            HelperLog.Warning(
                HelperLog.AppIds.ClosedXml,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Sheet,
                "Neutralized a formula-like text cell.");
        }

        return neutralized.Changed;
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
        if (lead is not ('=' or '+' or '-' or '@'))
            return (text, false);
        return ("'" + text, true);
    }
}
