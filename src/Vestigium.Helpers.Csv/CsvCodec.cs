using System.Globalization;
using System.Text;
using Vestigium.Helpers;
using Vestigium.Logging;

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
        HelperGuard.NotNull(table, nameof(table));
        var width = table.Headers.Count;
        if (options.HasHeaderRow && width == 0)
            throw new ArgumentException("A header row needs at least one column.", nameof(table));

        var sb = new StringBuilder();
        if (options.HasHeaderRow)
            WriteRecord(sb, table.Headers.Select(h => (object?)h).ToArray(), width, options, rowIndex: -1);

        var i = 0;
        foreach (var row in table.Rows)
        {
            HelperGuard.NotNull(row, nameof(table.Rows));
            if (row.Count > width && width > 0)
            {
                HelperLog.Reject($"row {i} has {row.Count} cells; header has {width}");
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
            if (options.HasHeaderRow)
                throw new CsvFormatException(1, "The file is empty and a header row was required.");
            return CsvTable.Empty();
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
                throw new CsvFormatException(line, $"Record has {record.Count} fields; expected {width}.");

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
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            case decimal m:
                return m.ToString(CultureInfo.InvariantCulture);
            case float f:
                HelperGuard.Finite(f, nameof(value));
                return f.ToString("G9", CultureInfo.InvariantCulture);
            case double d:
                HelperGuard.Finite(d, nameof(value));
                return d.ToString("G17", CultureInfo.InvariantCulture);
            case DateTime dt:
                return dt.Kind == DateTimeKind.Utc
                    ? dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
                    : dt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
            case DateTimeOffset dto:
                return dto.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            case TimeSpan ts:
                return ts.ToString("c", CultureInfo.InvariantCulture);
            default:
                var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                return neutralize ? Neutralize(text) : text;
        }
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
        var needs = false;
        foreach (var c in field)
        {
            if (c == delimiter || c == CsvOptions.Quote || c is '\r' or '\n')
            {
                needs = true;
                break;
            }
        }

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
                HelperLog.Verbose(
                    HelperLog.CurrentAppId,
                    VestigiumStatus.Success,
                    HelperLog.Subcategories.Session,
                    $"Neutralized a formula-like text cell row={rowIndex} col={c}");
            }

            sb.Append(Quote(text, options.Delimiter));
        }

        sb.Append("\r\n");
    }

    private static bool IsTextual(object? value) => value is string || value is not (
        null or bool or byte or sbyte or short or ushort or int or uint
        or long or ulong or decimal or float or double or DateTime or DateTimeOffset or TimeSpan);

    private static string DecodeUtf8(byte[] bytes)
    {
        var start = 0;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            start = 3;
        return Utf8NoBom.GetString(bytes, start, bytes.Length - start);
    }

    private static List<string> Trim(List<string> fields, CsvOptions options)
    {
        if (!options.TrimFields)
            return fields;
        return fields.Select(f => f.Trim()).ToList();
    }

    private static List<List<string>> ParseRecords(string text, char delimiter)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var i = 0;
        var line = 1;
        var inQuotes = false;
        var fieldStart = true;
        var sawRecord = false;

        void EndField()
        {
            record.Add(field.ToString());
            field.Clear();
            fieldStart = true;
        }

        void EndRecord()
        {
            EndField();
            records.Add(record);
            record = [];
            line++;
            fieldStart = true;
            sawRecord = true;
        }

        while (i < text.Length)
        {
            var c = text[i];
            if (inQuotes)
            {
                if (c == CsvOptions.Quote)
                {
                    if (i + 1 < text.Length && text[i + 1] == CsvOptions.Quote)
                    {
                        field.Append(CsvOptions.Quote);
                        i += 2;
                        fieldStart = false;
                        continue;
                    }

                    inQuotes = false;
                    i++;
                    fieldStart = false;
                    continue;
                }

                field.Append(c);
                i++;
                fieldStart = false;
                continue;
            }

            if (c == CsvOptions.Quote && fieldStart)
            {
                inQuotes = true;
                i++;
                fieldStart = false;
                continue;
            }

            if (c == CsvOptions.Quote)
                throw new CsvFormatException(line, "A quote appeared in the middle of an unquoted field.");

            if (c == delimiter)
            {
                EndField();
                i++;
                continue;
            }

            if (c is '\r' or '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;
                i++;
                if (i >= text.Length && record.Count == 0 && field.Length == 0 && sawRecord)
                    break;
                EndRecord();
                continue;
            }

            field.Append(c);
            fieldStart = false;
            i++;
        }

        if (inQuotes)
            throw new CsvFormatException(line, "Unclosed quoted field at end of file.");

        if (field.Length > 0 || record.Count > 0)
            EndRecord();

        return records;
    }
}
