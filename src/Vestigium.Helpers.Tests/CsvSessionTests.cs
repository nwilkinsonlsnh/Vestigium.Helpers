using System.Text;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Csv;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class CsvSessionTests
{
    [Fact]
    public void Identity_is_stable()
        => Assert.Equal("Vestigium.Helpers.Csv", CsvHelper.Identity);

    [Fact]
    public void WriteTable_round_trips_known_bytes()
    {
        var path = TempCsv();
        var table = CsvTable.Create(
            ["Name", "Value"],
            [
                ["Ada", 1],
                ["Bob", 2]
            ]);
        CsvHelper.WriteTable(table, path, new CsvOptions { Utf8Bom = false });

        var text = File.ReadAllText(path, new UTF8Encoding(false));
        Assert.Equal("Name,Value\r\nAda,1\r\nBob,2\r\n", text);

        var back = CsvHelper.Read(path, new CsvOptions { Utf8Bom = false });
        Assert.Equal(["Name", "Value"], back.Headers);
        Assert.Equal(2, back.Rows.Count);
        Assert.Equal("Ada", back.Rows[0][0]);
        Assert.Equal("1", back.Rows[0][1]);
        Assert.Equal("Bob", back.Rows[1][0]);
        Assert.Equal("2", back.Rows[1][1]);
    }

    [Fact]
    public void Embedded_comma_is_quoted()
    {
        var path = TempCsv();
        CsvHelper.WriteTable(
            CsvTable.Create(["Note"], [["a,b"]]),
            path,
            new CsvOptions { Utf8Bom = false });
        Assert.Equal("Note\r\n\"a,b\"\r\n", File.ReadAllText(path));
        Assert.Equal("a,b", CsvHelper.Read(path).Rows[0][0]);
    }

    [Fact]
    public void Embedded_quote_is_doubled()
    {
        var path = TempCsv();
        CsvHelper.WriteTable(
            CsvTable.Create(["Note"], [["say \"hi\""]]),
            path,
            new CsvOptions { Utf8Bom = false });
        Assert.Equal("Note\r\n\"say \"\"hi\"\"\"\r\n", File.ReadAllText(path));
        Assert.Equal("say \"hi\"", CsvHelper.Read(path).Rows[0][0]);
    }

    [Fact]
    public void Embedded_crlf_survives_round_trip()
    {
        var path = TempCsv();
        CsvHelper.WriteTable(
            CsvTable.Create(["Note"], [["line1\r\nline2"]]),
            path,
            new CsvOptions { Utf8Bom = false });
        Assert.Contains("\"line1\r\nline2\"", File.ReadAllText(path));
        Assert.Equal("line1\r\nline2", CsvHelper.Read(path).Rows[0][0]);
    }

    [Fact]
    public void Header_spaces_are_not_trimmed()
    {
        var path = TempCsv();
        CsvHelper.WriteTable(
            CsvTable.Create([" Name "], [["x"]]),
            path,
            new CsvOptions { Utf8Bom = false });
        Assert.Equal(" Name ", CsvHelper.Read(path).Headers[0]);
    }

    [Fact]
    public void Extra_cells_on_write_throw()
    {
        using var file = CsvHelper.Create("Csv", new CsvOptions { Utf8Bom = false });
        Assert.Throws<ArgumentException>(() =>
            file.WriteTable(CsvTable.Create(["A"], [["one", "two"]])));
    }

    [Fact]
    public void Extra_cells_on_read_throw_with_line_number()
    {
        var path = TempCsv();
        File.WriteAllText(path, "A,B\r\n1,2,3\r\n");
        var ex = Assert.Throws<CsvFormatException>(() => CsvHelper.Read(path));
        Assert.Equal(2, ex.LineNumber);
    }

    [Fact]
    public void Formula_like_text_gets_a_leading_apostrophe()
    {
        var path = TempCsv();
        CsvHelper.WriteTable(
            CsvTable.Create(["Payload"], [["=1+1"], ["+2"], ["-SUM(A1)"], ["@cmd"]]),
            path,
            new CsvOptions { Utf8Bom = false });
        var text = File.ReadAllText(path);
        Assert.Contains("'=1+1", text);
        var back = CsvHelper.Read(path);
        Assert.Equal("'=1+1", back.Rows[0][0]);
        Assert.Equal("'+2", back.Rows[1][0]);
        Assert.Equal("'-SUM(A1)", back.Rows[2][0]);
        Assert.Equal("'@cmd", back.Rows[3][0]);
    }

    [Fact]
    public void Numeric_negative_is_not_prefixed()
    {
        var path = TempCsv();
        CsvHelper.WriteTable(
            CsvTable.Create(["X"], [[-1.25m]]),
            path,
            new CsvOptions { Utf8Bom = false });
        Assert.Equal("X\r\n-1.25\r\n", File.ReadAllText(path));
    }

    [Fact]
    public void NaN_is_rejected()
    {
        using var file = CsvHelper.Create("Csv");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            file.WriteTable(CsvTable.Create(["X"], [[double.NaN]])));
    }

    [Fact]
    public void Infinity_is_rejected()
    {
        using var file = CsvHelper.Create("Csv");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            file.WriteTable(CsvTable.Create(["X"], [[double.PositiveInfinity]])));
    }

    [Fact]
    public void Utf8_non_ascii_round_trips()
    {
        var path = TempCsv();
        CsvHelper.WriteTable(
            CsvTable.Create(["Word"], [["café"], ["Δ"]]),
            path,
            new CsvOptions { Utf8Bom = false });
        var back = CsvHelper.Read(path);
        Assert.Equal("café", back.Rows[0][0]);
        Assert.Equal("Δ", back.Rows[1][0]);
    }

    [Fact]
    public void Default_write_starts_with_utf8_bom()
    {
        var path = TempCsv();
        CsvHelper.WriteTable(CsvTable.Create(["A"], [["1"]]), path);
        var bytes = File.ReadAllBytes(path);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
        Assert.Equal("1", CsvHelper.Read(path).Rows[0][0]);
    }

    [Fact]
    public void Reader_accepts_lf_only()
    {
        var path = TempCsv();
        File.WriteAllText(path, "Name,Value\nAda,1\n");
        var back = CsvHelper.Read(path);
        Assert.Equal("Ada", back.Rows[0][0]);
    }

    [Fact]
    public void Tab_dialect_quotes_embedded_tabs()
    {
        var path = TempCsv();
        CsvHelper.WriteTable(
            CsvTable.Create(["A", "B"], [["x\ty", 2]]),
            path,
            new CsvOptions { Delimiter = '\t', Utf8Bom = false });
        Assert.Equal("A\tB\r\n\"x\ty\"\t2\r\n", File.ReadAllText(path));
        var back = CsvHelper.Read(path, CsvOptions.Tab);
        Assert.Equal("x\ty", back.Rows[0][0]);
    }

    [Fact]
    public void Pipe_preset_round_trips()
    {
        var path = TempCsv();
        CsvHelper.WriteTable(
            CsvTable.Create(["A", "B"], [["one", "two"]]),
            path,
            CsvOptions.Pipe with { Utf8Bom = false });
        Assert.Equal("A|B\r\none|two\r\n", File.ReadAllText(path));
        var back = CsvHelper.Read(path, CsvOptions.WithDelimiter('|'));
        Assert.Equal("two", back.Rows[0][1]);
    }

    [Fact]
    public void Semicolon_preset_round_trips()
    {
        var path = TempCsv();
        CsvHelper.WriteTable(
            CsvTable.Create(["A", "B"], [["one", "two"]]),
            path,
            CsvOptions.Semicolon with { Utf8Bom = false });
        Assert.Equal("A;B\r\none;two\r\n", File.ReadAllText(path));
    }

    [Fact]
    public void Illegal_delimiter_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CsvOptions.WithDelimiter('"'));
        Assert.Throws<ArgumentOutOfRangeException>(() => CsvOptions.WithDelimiter('\n'));
        Assert.Throws<ArgumentOutOfRangeException>(() => CsvOptions.WithDelimiter('\r'));
        Assert.Throws<ArgumentOutOfRangeException>(() => CsvOptions.WithDelimiter('\0'));
    }

    [Fact]
    public void WriteSeries_writes_index_value_timestamp()
    {
        var path = TempCsv();
        var origin = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var series = NumericSeries.FromObservations(
            [
                new Observation(10m, origin),
                new Observation(11m, origin.AddSeconds(1)),
                new Observation(12m, origin.AddSeconds(2))
            ],
            "three");
        CsvHelper.WriteSeries(series, path, new CsvOptions { Utf8Bom = false });
        var back = CsvHelper.Read(path);
        Assert.Equal(["Index", "Value", "Timestamp"], back.Headers);
        Assert.Equal(3, back.Rows.Count);
        Assert.Equal("0", back.Rows[0][0]);
        Assert.Equal("10", back.Rows[0][1]);
        Assert.Contains("2026-09-08T12:00:00.000Z", (string)back.Rows[0][2]!);
    }

    [Fact]
    public void Open_missing_file_throws()
        => Assert.Throws<FileNotFoundException>(() =>
            CsvHelper.Open(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv")));

    [Fact]
    public void Default_export_path_does_not_create_the_folder()
    {
        var dir = CsvHelper.DefaultExportDirectory("Csv");
        Assert.Contains(Path.Combine("Vestigium", "Exports", "Csv"), dir);
        var path = CsvHelper.NewExportPath("Csv");
        Assert.StartsWith(dir, path);
        Assert.EndsWith(".csv", path);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Probe_does_not_create_a_desktop_file()
    {
        var before = Directory.Exists(CsvHelper.DefaultExportDirectory("Csv"))
            ? Directory.GetFiles(CsvHelper.DefaultExportDirectory("Csv"), "*.csv")
            : [];
        Assert.Equal("Vestigium.Helpers.Csv", CsvHelper.Probe());
        var after = Directory.Exists(CsvHelper.DefaultExportDirectory("Csv"))
            ? Directory.GetFiles(CsvHelper.DefaultExportDirectory("Csv"), "*.csv")
            : [];
        Assert.Equal(before.Length, after.Length);
    }

    [Fact]
    public void Session_create_write_save_open_read()
    {
        var path = TempCsv();
        using (var file = CsvHelper.Create("Csv", new CsvOptions { Utf8Bom = false }))
        {
            file.WriteTable(CsvTable.Create(["ms", "utc"], [["12.4", "now"], ["11.9", "then"]]));
            file.SaveAs(path);
        }

        using var opened = CsvHelper.Open(path, "Csv");
        var table = opened.Read();
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("12.4", table.Rows[0][0]);
    }

    [Fact]
    public void AppendRows_extends_the_table()
    {
        using var file = CsvHelper.Create("Csv", new CsvOptions { Utf8Bom = false });
        file.WriteTable(CsvTable.Create(["N"], [[1]]));
        file.AppendRows([[2], [3]]);
        var path = TempCsv();
        file.SaveAs(path);
        Assert.Equal(3, CsvHelper.Read(path).Rows.Count);
    }

    [Fact]
    public void Unclosed_quote_throws()
    {
        var path = TempCsv();
        File.WriteAllText(path, "A\r\n\"no end\r\n");
        Assert.Throws<CsvFormatException>(() => CsvHelper.Read(path));
    }

    private static string TempCsv()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "sample.csv");
    }
}
