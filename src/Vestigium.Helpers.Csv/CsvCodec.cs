using System.Globalization;
using System.Text;

namespace Vestigium.Helpers.Csv;

internal static class CsvCodec
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static byte[] WriteBytes(CsvTable table, CsvOptions options)
    {
        var text = WriteText(table, options);
        var body = Utf8NoBom.GetBytes(text);
        if (!options.Utf8Bom)
            return body;

        var bom = Encoding.UTF8.GetPreamble();
        var buffer = new byte[bom.Length + body.Length];
        bom.CopyTo(buffer, 0);
        body.CopyTo(buffer, bom.Length);
        return buffer;
    }

    public static string WriteText(CsvTable table, CsvOptions options)
    {
        ArgumentNullException.ThrowIfNull(table);
        var width = table.Headers.Count;
        if (options.HasHeaderRow && width == 0)
        {
            CsvLog.Error(CsvEvents.WriteRejected, CsvCatalog.Subcategories.Session, "rejected write",
                properties: CsvLog.Props(("reason", "empty-header")));
            throw new ArgumentException("A header row needs at least one column.", nameof(table));
        }

        var sb = new StringBuilder();
        if (options.HasHeaderRow)
            WriteRecord(sb, table.Headers.Select(h => (object?)h).ToArray(), width, options, rowIndex: -1);

        var i = 0;
        foreach (var row in table.Rows)
        {
            ArgumentNullException.ThrowIfNull(row);
            if (row.Count > width && width > 0)
            {
                CsvLog.Error(CsvEvents.WriteRejected, CsvCatalog.Subcategories.Session, "rejected write",
                    properties: CsvLog.Props(("reason", "row-width"), ("row", i.ToString()), ("cells", row.Count.ToString()), ("header", width.ToString())));
                throw new ArgumentException($"Row {i} has {row.Count} cells; the header has {width}.", nameof(table));
            }

            WriteRecord(sb, row, width, options, i);
            i++;
        }

        return sb.ToString();
    }

    public static CsvTable ReadBytes(byte[] bytes, CsvOptions options)
    {
        var text = DecodeUtf8(bytes);
        return ReadText(text, options);
    }

    public static CsvTable ReadText(string text, CsvOptions options)
    {
        if (text.Length > 0 && text[0] == '\uFEFF')
            text = text[1..];

        var records = ParseRecords(text, options.Delimiter);
        if (records.Count == 0)
        {
            return options.HasHeaderRow ? throw Fail(1, "The file is empty and a header row was required.") : CsvTable.Empty();
        }

        var width = records[0].Count;
        IReadOnlyList<string> headers;
        var start = 0;
        if (options.HasHeaderRow)
        {
            headers = Trim(records[0], options);
            start = 1;
        }
        else
        {
            headers = Enumerable.Range(1, width).Select(n => "F" + n).ToArray();
        }

        var rows = new List<IReadOnlyList<object?>>(Math.Max(0, records.Count - start));
        for (var r = start; r < records.Count; r++)
        {
            var record = records[r];
            var line = r + 1;
            if (record.Count > width)
                throw Fail(line, $"Record has {record.Count} fields; expected {width}.");

            var cells = new object?[width];
            for (var c = 0; c < width; c++)
            {
                if (c >= record.Count)
                {
                    cells[c] = null;
                    continue;
                }

                var field = record[c];
                if (options.TrimFields)
                    field = field.Trim();
                cells[c] = field;
            }

            rows.Add(cells);
        }

        return new CsvTable { Headers = headers, Rows = rows };
    }

    public static string FormatCell(object? value, bool neutralize)
    {
        switch (value)
        {
            case null:
                return string.Empty;
            case string s:
                return neutralize ? Neutralize(s) : s;
            case bool b:
                return b ? "true" : "false";
            case byte or sbyte or short or ushort or int or uint or long or ulong:
                return FormatInteger(value);
            case decimal or float or double:
                return FormatFloating(value);
            case DateTime or DateTimeOffset or TimeSpan:
                return FormatDateTime(value);
            default:
                var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                return neutralize ? Neutralize(text) : text;
        }
    }

    internal static string FormatInteger(object value)
        => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    internal static string FormatFloating(object value)
    {
        switch (value)
        {
            case decimal m:
                return m.ToString(CultureInfo.InvariantCulture);
            case float f when !float.IsFinite(f):
            {
                CsvLog.Error(CsvEvents.CellRejectedNonFinite, CsvCatalog.Subcategories.Session, "rejected non-finite number");
                throw new ArgumentOutOfRangeException(nameof(value), "Value is not a finite number.");
            }
            case float f:
                return f.ToString("G9", CultureInfo.InvariantCulture);
            case double d when !double.IsFinite(d):
            {
                CsvLog.Error(CsvEvents.CellRejectedNonFinite, CsvCatalog.Subcategories.Session, "rejected non-finite number");
                    throw new ArgumentOutOfRangeException(nameof(value), "Value is not a finite number.");
                }
                return d.ToString("G17", CultureInfo.InvariantCulture);
            default:
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }

    internal static string FormatDateTime(object value)
    {
        return value switch
        {
            DateTime dt => dt.Kind == DateTimeKind.Utc
                ? dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
                : dt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            TimeSpan ts => ts.ToString("c", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    public static string Neutralize(string text)
    {
        if (text.Length == 0)
            return text;
        var lead = text[0];
        if (lead is not ('=' or '+' or '-' or '@'))
            return text;
        return "'" + text;
    }

    public static string Quote(string field, char delimiter)
    {
        var needs = field.Any(c => c == delimiter || c == CsvOptions.Quote || c is '\r' or '\n');

        if (!needs)
            return field;

        return "\"" + field.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static void WriteRecord(
        StringBuilder sb,
        IReadOnlyList<object?> row,
        int width,
        CsvOptions options,
        int rowIndex)
    {
        var count = width == 0 ? row.Count : width;
        for (var c = 0; c < count; c++)
        {
            if (c > 0)
                sb.Append(options.Delimiter);
            var raw = c < row.Count ? row[c] : null;
            var text = FormatCell(raw, options.NeutralizeInjection && IsTextual(raw));
            if (options.NeutralizeInjection && IsTextual(raw) && text.Length > 0 && text[0] == '\'')
            {
                CsvLog.Warning(
                    CsvEvents.CellNeutralized,
                    CsvCatalog.Subcategories.Session,
                    "neutralized formula-like text",
                    properties: CsvLog.Props(("row", rowIndex.ToString()), ("col", c.ToString())));
            }

            sb.Append(Quote(text, options.Delimiter));
        }

        sb.Append("\r\n");
    }

    private static bool IsTextual(object? value) => value is string or not (
        null or bool or byte or sbyte or short or ushort or int or uint
        or long or ulong or decimal or float or double or DateTime or DateTimeOffset or TimeSpan);

    private static string DecodeUtf8(byte[] bytes)
    {
        var start = 0;
        if (bytes is [0xEF, 0xBB, 0xBF, ..])
            start = 3;
        return Utf8NoBom.GetString(bytes, start, bytes.Length - start);
    }

    private static List<string> Trim(List<string> fields, CsvOptions options)
    {
        return !options.TrimFields ? fields : fields.Select(f => f.Trim()).ToList();
    }

    private static CsvFormatException Fail(int line, string message)
    {
        CsvLog.Error(
            CsvEvents.ParseRejected,
            CsvCatalog.Subcategories.Parse,
            "rejected parse",
            properties: CsvLog.Props(("line", line.ToString()), ("reason", message)));
        return new CsvFormatException(line, message);
    }

    private static List<List<string>> ParseRecords(string text, char delimiter)
    {
        var records = new List<List<string>>( );
        var record = new List<string>( );
        var field = new StringBuilder( );

        var line = 1;
        var inQuotes = false;
        var isFieldStart = true;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (inQuotes)
            {
                if (c == CsvOptions.Quote)
                {
                    // Lookahead: Escaped quote ("")
                    if (i + 1 < text.Length && text[i + 1] == CsvOptions.Quote)
                    {
                        field.Append(CsvOptions.Quote);
                        i++; // Skip the second quote
                    }
                    else
                    {
                        inQuotes = false; // Close quotes
                    }
                }
                else
                {
                    field.Append(c);
                }
                isFieldStart = false;
            }
            else
            {
                if (c == CsvOptions.Quote)
                {
                    if (!isFieldStart)
                        throw Fail(line, "A quote appeared in the middle of an unquoted field.");

                    inQuotes = true;
                    isFieldStart = false;
                }
                else if (c == delimiter)
                {
                    EndField( );
                }
                else if (c is '\r' or '\n')
                {
                    // Lookahead: Windows newline (\r\n)
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;

                    // Ignore a completely empty trailing newline at the end of the file
                    if (i == text.Length - 1 && record.Count == 0 && field.Length == 0 && records.Count > 0)
                        break;

                    EndRecord( );
                }
                else
                {
                    field.Append(c);
                    isFieldStart = false;
                }
            }
        }

        if (inQuotes)
            throw Fail(line, "Unclosed quoted field at end of file.");

        // Flush remaining data at EOF
        if (field.Length > 0 || record.Count > 0)
            EndRecord( );

        return records;

        void EndRecord( )
        {
            EndField( );
            records.Add(record);
            record = [ ];
            line++;
        }

        void EndField( )
        {
            record.Add(field.ToString( ));
            field.Clear( );
            isFieldStart = true; // Next character will start a new field
        }
    }
}
