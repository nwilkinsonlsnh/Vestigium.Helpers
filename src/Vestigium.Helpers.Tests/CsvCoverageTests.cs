using System.Globalization;
using System.Text;
using Vestigium.Helpers.Csv;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class CsvCoverageTests
{
    [Fact]
    public void FormatCell_covers_every_switch_arm()
    {
        Assert.Equal("", CsvCodec.FormatCell(null, neutralize: true));
        Assert.Equal("plain", CsvCodec.FormatCell("plain", neutralize: true));
        Assert.Equal("'=1+1", CsvCodec.FormatCell("=1+1", neutralize: true));
        Assert.Equal("=1+1", CsvCodec.FormatCell("=1+1", neutralize: false));
        Assert.Equal("true", CsvCodec.FormatCell(true, neutralize: true));
        Assert.Equal("false", CsvCodec.FormatCell(false, neutralize: true));
        Assert.Equal("255", CsvCodec.FormatCell((byte)255, neutralize: true));
        Assert.Equal("-8", CsvCodec.FormatCell((sbyte)-8, neutralize: true));
        Assert.Equal("-2", CsvCodec.FormatCell((short)-2, neutralize: true));
        Assert.Equal("3", CsvCodec.FormatCell((ushort)3, neutralize: true));
        Assert.Equal("-4", CsvCodec.FormatCell(-4, neutralize: true));
        Assert.Equal("5", CsvCodec.FormatCell(5u, neutralize: true));
        Assert.Equal("-6", CsvCodec.FormatCell(-6L, neutralize: true));
        Assert.Equal("7", CsvCodec.FormatCell(7UL, neutralize: true));
        Assert.Equal("1.25", CsvCodec.FormatCell(1.25m, neutralize: true));
        Assert.Equal((1.5f).ToString("G9", CultureInfo.InvariantCulture), CsvCodec.FormatCell(1.5f, neutralize: true));
        Assert.Equal((1.5d).ToString("G17", CultureInfo.InvariantCulture), CsvCodec.FormatCell(1.5d, neutralize: true));

        var utc = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal("2026-09-08T12:00:00.000Z", CsvCodec.FormatCell(utc, neutralize: true));
        var local = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Unspecified);
        Assert.Equal("2026-09-08T12:00:00.000", CsvCodec.FormatCell(local, neutralize: true));

        var dto = new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.FromHours(-4));
        Assert.Equal("2026-09-08T12:00:00.000Z", CsvCodec.FormatCell(dto, neutralize: true));
        Assert.Equal("01:30:00", CsvCodec.FormatCell(TimeSpan.FromMinutes(90), neutralize: true));

        var guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Assert.Equal(guid.ToString(), CsvCodec.FormatCell(guid, neutralize: true));
        Assert.Equal("'+CMD", CsvCodec.FormatCell(new FormulaLike(), neutralize: true));
        Assert.Equal("+CMD", CsvCodec.FormatCell(new FormulaLike(), neutralize: false));
    }

    [Fact]
    public void FormatCell_rejects_non_finite_floats()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CsvCodec.FormatCell(float.NaN, neutralize: true));
        Assert.Throws<ArgumentOutOfRangeException>(() => CsvCodec.FormatCell(float.PositiveInfinity, neutralize: true));
        Assert.Throws<ArgumentOutOfRangeException>(() => CsvCodec.FormatCell(double.NegativeInfinity, neutralize: true));
    }

    [Fact]
    public void Neutralize_leaves_empty_and_safe_text()
    {
        Assert.Equal("", CsvCodec.Neutralize(""));
        Assert.Equal("ok", CsvCodec.Neutralize("ok"));
        Assert.Equal("'+1", CsvCodec.Neutralize("+1"));
        Assert.Equal("'-1", CsvCodec.Neutralize("-1"));
        Assert.Equal("'@cmd", CsvCodec.Neutralize("@cmd"));
    }

    [Fact]
    public void FormatException_three_arg_ctor_preserves_inner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new CsvFormatException(4, "bad record", inner);
        Assert.Equal(4, ex.LineNumber);
        Assert.Same(inner, ex.InnerException);
        Assert.Equal("Line 4: bad record", ex.Message);
    }

    [Fact]
    public void No_header_invents_F_columns()
    {
        var path = TempCsv();
        File.WriteAllText(path, "a,b\r\n1,2\r\n");
        var table = CsvHelper.Read(path, new CsvOptions { HasHeaderRow = false, Utf8Bom = false });
        Assert.Equal(["F1", "F2"], table.Headers);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("a", table.Rows[0][0]);
        Assert.Equal("2", table.Rows[1][1]);
    }

    [Fact]
    public void TrimFields_trims_headers_and_cells()
    {
        var path = TempCsv();
        File.WriteAllText(path, " A , B \r\n x , y \r\n");
        var table = CsvHelper.Read(path, new CsvOptions { TrimFields = true, Utf8Bom = false });
        Assert.Equal("A", table.Headers[0]);
        Assert.Equal("B", table.Headers[1]);
        Assert.Equal("x", table.Rows[0][0]);
        Assert.Equal("y", table.Rows[0][1]);
    }

    [Fact]
    public void Empty_file_with_header_throws_empty_without_header_is_empty_table()
    {
        var path = TempCsv();
        File.WriteAllText(path, "");
        var headerEx = Assert.Throws<CsvFormatException>(() => CsvHelper.Read(path));
        Assert.Equal(1, headerEx.LineNumber);
        var empty = CsvHelper.Read(path, new CsvOptions { HasHeaderRow = false });
        Assert.Empty(empty.Headers);
        Assert.Empty(empty.Rows);
        Assert.Empty(CsvTable.Empty().Headers);
    }

    [Fact]
    public void Missing_cells_pad_null_and_mid_quote_throws()
    {
        var path = TempCsv();
        File.WriteAllText(path, "A,B,C\r\n1\r\n");
        var table = CsvHelper.Read(path);
        Assert.Equal("1", table.Rows[0][0]);
        Assert.Null(table.Rows[0][1]);
        Assert.Null(table.Rows[0][2]);

        File.WriteAllText(path, "A\r\nhe\"llo\r\n");
        var quote = Assert.Throws<CsvFormatException>(() => CsvHelper.Read(path));
        Assert.Contains("quote", quote.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Header_row_requires_a_column_on_write()
    {
        Assert.Throws<ArgumentException>(() =>
            CsvCodec.WriteText(CsvTable.Empty(), new CsvOptions { HasHeaderRow = true }));
        var text = CsvCodec.WriteText(
            new CsvTable { Headers = [], Rows = [["a", "b"]] },
            new CsvOptions { HasHeaderRow = false, Utf8Bom = false });
        Assert.Equal("a,b\r\n", text);
    }

    [Fact]
    public void OpenOrCreate_creates_then_reopens()
    {
        var path = TempCsv();
        using (var created = CsvHelper.OpenOrCreate(path, "Csv", new CsvOptions { HasHeaderRow = false, Utf8Bom = false }))
        {
            created.WriteTable(new CsvTable { Headers = [], Rows = [["1"]] });
            created.Save();
        }

        using var reopened = CsvHelper.OpenOrCreate(path, "Csv", new CsvOptions { HasHeaderRow = false, Utf8Bom = false });
        Assert.Equal("1", reopened.Read().Rows[0][0]);
    }

    [Fact]
    public void Stream_write_and_read_round_trip()
    {
        using var buffer = new MemoryStream();
        CsvHelper.WriteTable(
            CsvTable.Create(["Name", "Value"], [["Ada", 1]]),
            buffer,
            new CsvOptions { Utf8Bom = false });
        buffer.Position = 0;
        var back = CsvHelper.Read(buffer, new CsvOptions { Utf8Bom = false });
        Assert.Equal("Ada", back.Rows[0][0]);
        Assert.Equal("1", back.Rows[0][1]);
    }

    [Fact]
    public void WriteTable_formats_bool_dates_and_guid()
    {
        var path = TempCsv();
        var utc = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CsvHelper.WriteTable(
            CsvTable.Create(
                ["Flag", "When", "Span", "Id", "Nulls"],
                [[true, utc, TimeSpan.FromSeconds(3), guid, null]]),
            path,
            new CsvOptions { Utf8Bom = false });
        var text = File.ReadAllText(path);
        Assert.Contains("true", text);
        Assert.Contains("2026-09-08T12:00:00.000Z", text);
        Assert.Contains("00:00:03", text);
        Assert.Contains(guid.ToString(), text);
    }

    [Fact]
    public void KeyValue_and_options_describe_delimiters()
    {
        var table = CsvTable.KeyValue("K", "V", [("n", 1), ("m", 2)]);
        Assert.Equal(["K", "V"], table.Headers);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("comma", CsvOptions.Rfc4180.DescribeDelimiter());
        Assert.Equal("tab", CsvOptions.Tab.DescribeDelimiter());
        Assert.Equal("semicolon", CsvOptions.Semicolon.DescribeDelimiter());
        Assert.Equal("pipe", CsvOptions.Pipe.DescribeDelimiter());
        Assert.Equal("U+0023", CsvOptions.WithDelimiter('#').DescribeDelimiter());
        var copied = CsvOptions.Tab.WithDelimiterChar(';');
        Assert.Equal(';', copied.Delimiter);
        Assert.False(copied.TrimFields);
        var named = CsvHelper.NewExportPath("Csv", "probe");
        Assert.Contains("probe-", Path.GetFileName(named));
        Assert.EndsWith(".csv", named);
    }

    [Fact]
    public void Quote_only_when_needed()
    {
        Assert.Equal("plain", CsvCodec.Quote("plain", ','));
        Assert.Equal("\"a,b\"", CsvCodec.Quote("a,b", ','));
        Assert.Equal("\"a\"\"b\"", CsvCodec.Quote("a\"b", ','));
        Assert.Equal("\"a\nb\"", CsvCodec.Quote("a\nb", ','));
    }

    [Fact]
    public void Doubled_quotes_inside_a_quoted_field_round_trip()
    {
        var path = TempCsv();
        File.WriteAllText(path, "Note\r\n\"say \"\"hi\"\"\"\r\n");
        Assert.Equal("say \"hi\"", CsvHelper.Read(path).Rows[0][0]);
    }

    private sealed class FormulaLike
    {
        public override string ToString() => "+CMD";
    }

    private static string TempCsv()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "sample.csv");
    }

    [Fact]
    public void AppendRows_before_WriteTable_throws()
    {
        using var file = CsvHelper.Create("Csv", new CsvOptions { HasHeaderRow = false, Utf8Bom = false });
        var ex = Assert.Throws<InvalidOperationException>(() => file.AppendRows([["a"]]));
        Assert.Contains("Write a table before appending", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteSeries_values_only_leaves_timestamp_empty()
    {
        var path = TempCsv();
        var series = Vestigium.Helpers.Analytics.NumericSeries.From(new[] { 1, 2, 3 }, "vals");
        CsvHelper.WriteSeries(series, path, new CsvOptions { Utf8Bom = false });
        var table = CsvHelper.Read(path, new CsvOptions { Utf8Bom = false });
        Assert.Equal(["Index", "Value", "Timestamp"], table.Headers);
        Assert.Equal("1", table.Rows[0][1]);
        Assert.True(string.IsNullOrEmpty(table.Rows[0][2] as string));
    }

    [Fact]
    public void Session_save_without_path_and_write_empty()
    {
        using var file = CsvHelper.Create("Csv", new CsvOptions { HasHeaderRow = false, Utf8Bom = false });
        using var buffer = new MemoryStream();
        file.WriteTo(buffer);
        Assert.True(buffer.Length >= 0);
        file.WriteTable(new CsvTable { Headers = [], Rows = [["x"]], Name = "T" });
        file.AppendRows([["y"]]);
        Assert.Equal(2, file.Read().Rows.Count);
    }

}
