using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using ClosedXML.Excel;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.ClosedXml;
using Vestigium.Helpers.Csv;
using Vestigium.Helpers.Encryption;
using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class CoverageHotspotTests
{
    [Fact]
    public void Linux_parsers_cover_neighbor_route_and_hex_arms()
    {
        Assert.Equal("Reachable", NetworkLinuxTables.NeighborState(0x02));
        Assert.Equal("Permanent", NetworkLinuxTables.NeighborState(0x04));
        Assert.Equal("Failed", NetworkLinuxTables.NeighborState(0x08));
        Assert.Equal("Incomplete", NetworkLinuxTables.NeighborState(0));
        Assert.Equal("Incomplete", NetworkLinuxTables.NeighborState(0x01));
        Assert.Equal(2, NetworkLinuxTables.ParseHex("0x2"));
        Assert.Equal(0, NetworkLinuxTables.ParseHex("zz"));
        Assert.Equal("0.0.0.0", NetworkLinuxTables.HexIpv4("nope"));
        Assert.Equal("192.168.1.1", NetworkLinuxTables.HexIpv4("0101A8C0"));
        Assert.Equal("short", NetworkLinuxTables.FormatIpv6Hex("short"));
        Assert.Equal("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", NetworkLinuxTables.FormatIpv6Hex("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz"));
        Assert.Equal("::1", NetworkLinuxTables.FormatIpv6Hex("00000000000000000000000000000001"));

        Assert.False(NetworkLinuxTables.TryParseNeighbor("bad", out _));
        Assert.True(NetworkLinuxTables.TryParseNeighbor(
            "192.168.1.1      0x1         0x2         aa:bb:cc:dd:ee:ff     *        eth0", out var reachable));
        Assert.Equal("Reachable", reachable.State);
        Assert.Equal("aa:bb:cc:dd:ee:ff", reachable.MacAddress);
        Assert.True(NetworkLinuxTables.TryParseNeighbor(
            "10.0.0.1 0x1 0x4 00:00:00:00:00:00 * eth0", out var permanent));
        Assert.Equal("Permanent", permanent.State);
        Assert.Null(permanent.MacAddress);
        Assert.True(NetworkLinuxTables.TryParseNeighbor(
            "10.0.0.2 0x1 0x8 11:22:33:44:55:66 * eth1", out var failed));
        Assert.Equal("Failed", failed.State);

        Assert.False(NetworkLinuxTables.TryParseIpv4Route("short", out _));
        Assert.True(NetworkLinuxTables.TryParseIpv4Route(
            "eth0\t00000000\t0100A8C0\t0003\t0\t0\t100\t00000000\t0\t0\t0", out var tab));
        Assert.Equal("eth0", tab.InterfaceName);
        Assert.Equal(100, tab.Metric);
        Assert.True(NetworkLinuxTables.TryParseIpv4Route(
            "eth1 0101A8C0 00000000 0001 0 0 x 00FFFFFF extra", out var space));
        Assert.Equal("eth1", space.InterfaceName);
        Assert.Equal(0, space.Metric);
        Assert.Equal(24, space.PrefixLength);

        Assert.False(NetworkLinuxTables.TryParseIpv6Route("short", out _));
        Assert.True(NetworkLinuxTables.TryParseIpv6Route(
            "00000000000000000000000000000001 80 00000000000000000000000000000000 00 00000000000000000000000000000000 00000064 00000000 00000000 00000001 lo",
            out var v6));
        Assert.Equal("::1", v6.Destination);
        Assert.Equal(0x80, v6.PrefixLength);
        Assert.Equal(0x64, v6.Metric);
        Assert.True(NetworkLinuxTables.TryParseIpv6Route(
            "shorthex 80 00 00 gw gg 00 00 00 01 eth9", out var shortHex));
        Assert.Equal("shorthex", shortHex.Destination);
        Assert.Equal(0, shortHex.Metric);
        Assert.Equal("eth9", shortHex.InterfaceName);
        Assert.True(NetworkLinuxTables.TryParseIpv6Route(
            "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz gg 00000000000000000000000000000000 00 zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz nothex 00000000 00000000 00000001 eth2",
            out var garbage));
        Assert.Equal(0, garbage.PrefixLength);
    }

    [Fact]
    public void Windows_neighbor_type_map_and_route_denied()
    {
        Assert.Equal("Dynamic", NetworkWindowsTables.NeighborType(3));
        Assert.Equal("Static", NetworkWindowsTables.NeighborType(4));
        Assert.Equal("Invalid", NetworkWindowsTables.NeighborType(2));
        Assert.Equal("Other", NetworkWindowsTables.NeighborType(0));
        Assert.Equal("Other", NetworkWindowsTables.NeighborType(9));

        var denied = NetworkRouteMutation.Denied("AddRoute", 5);
        Assert.Contains("Administrator", denied.Message, StringComparison.Ordinal);
        var invalid = NetworkRouteMutation.Denied("ChangeRoute", 87);
        Assert.Contains("invalid", invalid.Message, StringComparison.OrdinalIgnoreCase);
        var other = NetworkRouteMutation.Denied("RemoveRoute", 99);
        Assert.Contains("Win32=99", other.Message, StringComparison.Ordinal);
        var persist = NetworkRouteMutation.PersistentName(new NetworkRouteChange
        {
            Destination = "10.0.0.0",
            PrefixLength = 8,
            Gateway = "10.0.0.1",
            Metric = 0
        });
        Assert.Equal("10.0.0.0,255.0.0.0,10.0.0.1,1", persist);
        Assert.True(NetworkRouteMutation.FirstIpv4Index() >= 1);

        Assert.NotNull(NetworkHelper.GetRoutes(RouteFamily.IPv4));
        Assert.NotNull(NetworkHelper.GetRoutes(RouteFamily.IPv6));
        var listening = NetworkHelper.GetConnections(new NetworkConnectionQuery { ListeningOnly = true });
        Assert.All(listening, c => Assert.Equal("Listen", c.State));
        var established = NetworkHelper.GetConnections(new NetworkConnectionQuery { EstablishedOnly = true });
        Assert.All(established, c => Assert.Equal("Established", c.State));
        Assert.NotNull(NetworkHelper.GetConnections(new NetworkConnectionQuery { Protocol = TransportProtocol.Udp }));
        Assert.NotNull(NetworkHelper.GetConnections(new NetworkConnectionQuery { Protocol = TransportProtocol.Tcp }));
    }

    [Fact]
    public void Icmp_decide_status_and_summaries_do_not_need_a_socket()
    {
        Assert.Equal(NetworkJobStatus.Cancelled, IcmpEchoEngine.DecideStatus(true, 0, 3, 1, false));
        Assert.Equal(NetworkJobStatus.Cancelled, IcmpEchoEngine.DecideStatus(true, 4, 2, 0, false));
        Assert.Equal(NetworkJobStatus.Success, IcmpEchoEngine.DecideStatus(true, 4, 4, 2, false));
        Assert.Equal(NetworkJobStatus.Success, IcmpEchoEngine.DecideStatus(false, 4, 4, 1, true));
        Assert.Equal(NetworkJobStatus.Failed, IcmpEchoEngine.DecideStatus(false, 4, 4, 0, true));
        Assert.Equal(NetworkJobStatus.TimedOut, IcmpEchoEngine.DecideStatus(false, 4, 4, 0, false));
        Assert.Equal(NetworkJobStatus.TimedOut, IcmpEchoEngine.DecideStatus(false, 0, 0, 0, false));

        var empty = IcmpEchoEngine.SummarizeTimes([]);
        Assert.Null(empty.Min);
        Assert.Null(empty.Max);
        Assert.Null(empty.Average);
        var times = IcmpEchoEngine.SummarizeTimes([10, 20, 30]);
        Assert.Equal(10, times.Min);
        Assert.Equal(30, times.Max);
        Assert.Equal(20, times.Average);

        IcmpEchoEngine.LogFinished(NetworkJobStatus.Success, "ok");
        IcmpEchoEngine.LogFinished(NetworkJobStatus.Cancelled, "stop");
        IcmpEchoEngine.LogFinished(NetworkJobStatus.Failed, "fail");
        IcmpEchoEngine.LogFinished(NetworkJobStatus.TimedOut, "to");

        var hopFail = new IcmpTraceHop(1, null, [new IcmpTraceProbe(1, 1, ProbeProtocol.Icmp, IcmpEchoStatus.ProtocolForbidden, null, 0, "x")]);
        var hopAddr = new IcmpTraceHop(1, "1.1.1.1", [new IcmpTraceProbe(1, 1, ProbeProtocol.Icmp, IcmpEchoStatus.ProtocolForbidden, "1.1.1.1", 1, "x")]);
        var hopOk = new IcmpTraceHop(1, "8.8.8.8", [new IcmpTraceProbe(1, 1, ProbeProtocol.Icmp, IcmpEchoStatus.Success, "8.8.8.8", 1, "ok")]);
        Assert.Equal(NetworkJobStatus.Cancelled, IcmpTraceEngine.DecideStatus(true, false, []));
        Assert.Equal(NetworkJobStatus.Success, IcmpTraceEngine.DecideStatus(false, true, [hopOk]));
        Assert.Equal(NetworkJobStatus.Failed, IcmpTraceEngine.DecideStatus(false, false, [hopFail]));
        Assert.Equal(NetworkJobStatus.TimedOut, IcmpTraceEngine.DecideStatus(false, false, [hopAddr]));
        Assert.Equal(NetworkJobStatus.TimedOut, IcmpTraceEngine.DecideStatus(false, false, []));
    }

    [Fact]
    public void Frequency_table_empty_flat_tie_and_trend()
    {
        Assert.Empty(FrequencyTable.Empty.HistogramTrend());
        Assert.Empty(FrequencyTable.Empty.Pareto());
        Assert.Null(FrequencyTable.Empty.Mode);
        Assert.False(FrequencyTable.Empty.HasUniqueMode);

        var flat = FrequencyTable.Build([5m, 5m, 5m], iqr: 0, min: 5, max: 5);
        Assert.True(flat.HasUniqueMode);
        Assert.Equal(5m, flat.Mode);
        Assert.Single(flat.Histogram);
        Assert.Single(flat.HistogramTrend());
        Assert.Single(flat.Pareto());

        var viaNullIqr = FrequencyTable.Build([1m, 2m], iqr: null, min: 1, max: 2);
        Assert.Single(viaNullIqr.Histogram);

        var tie = FrequencyTable.Build([1m, 1m, 2m, 2m], iqr: 1, min: 1, max: 2);
        Assert.False(tie.HasUniqueMode);
        Assert.Null(tie.Mode);
        Assert.Equal(2, tie.Modes.Count);

        var unique = FrequencyTable.Build([1m, 1m, 2m, 3m], iqr: 1, min: 1, max: 3);
        Assert.True(unique.HasUniqueMode);
        Assert.Equal(1m, unique.Mode);

        var spread = FrequencyTable.Build(Enumerable.Range(1, 20).Select(i => (decimal)i).ToArray(), 10m, 1m, 20m);
        Assert.True(spread.Histogram.Count >= 2);
        Assert.Equal(spread.Histogram.Count, spread.HistogramTrend().Count);
        Assert.Equal(spread.Histogram.Count, spread.Pareto().Count);

        var gapped = FrequencyTable.Build([1m, 1m, 1m, 50m], iqr: 1, min: 1, max: 50);
        Assert.Contains(gapped.Histogram, b => b.Count == 0);
        Assert.NotEmpty(gapped.Pareto());
        var negativeIqr = FrequencyTable.Build([1m, 2m, 9m], iqr: -2, min: 1, max: 9);
        Assert.NotEmpty(negativeIqr.Histogram);

        Assert.Null(DescriptiveStatistics.ComputeSkewness([1m, 2m], 1.5, 0.7));
        Assert.Null(DescriptiveStatistics.ComputeSkewness([1m, 2m, 3m], 2, 0));
        Assert.Null(DescriptiveStatistics.ComputeSkewness([1m, 2m, 3m], 2, null));
        Assert.NotNull(DescriptiveStatistics.ComputeSkewness([1m, 2m, 3m], 2, 1));
        Assert.Null(DescriptiveStatistics.ComputeExcessKurtosis([1m, 2m, 3m], 2, 1));
        Assert.Null(DescriptiveStatistics.ComputeExcessKurtosis([1m, 2m, 3m, 4m], 2.5, 0));
        Assert.NotNull(DescriptiveStatistics.ComputeExcessKurtosis([1m, 2m, 3m, 4m], 2.5, 1.3));
        Assert.Null(DescriptiveStatistics.Compute([1m]).Skewness);
        Assert.Null(DescriptiveStatistics.Compute([0m, 0m]).CoefficientOfVariation);
        Assert.Null(DescriptiveStatistics.Compute([5m]).StdDev);
        Assert.Null(DescriptiveStatistics.Compute([-1m, 0m, 2m]).GeometricMean);
    }

    [Fact]
    public void Cell_writer_and_reader_cover_remaining_formats()
    {
        using var book = WorkbookHelper.Create("Mix", "ClosedXml");
        var when = new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Unspecified);
        var dto = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.FromHours(-4));
        book.Sheet("Mix").WriteTable(
            SheetTable.Create(
                ["Ulong", "Big", "Dto", "Span", "Fmt", "Guid", "Formula"],
                [[7UL, ulong.MaxValue, dto, TimeSpan.FromHours(26), when, Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), "=1+1"]]),
            new SheetWriteOptions
            {
                DateFormat = "yyyy-mm-dd hh:mm:ss",
                NumberFormat = "0.00",
                CreateExcelTable = false
            });
        var table = book.Sheet("Mix").ReadUsedRange();
        Assert.Single(table.Rows);

        var ws = book.Workbook.Worksheet("Mix");
        Assert.False(CellWriter.Write(ws.Cell(10, 1), null, new SheetWriteOptions()));
        Assert.False(CellWriter.WriteBool(ws.Cell(10, 2), true));
        Assert.False(CellWriter.WriteDate(ws.Cell(10, 3), when, new SheetWriteOptions()));
        Assert.False(CellWriter.WriteDate(ws.Cell(10, 4), dto, new SheetWriteOptions { DateFormat = "yyyy-mm-dd" }));
        Assert.False(CellWriter.WriteDate(ws.Cell(10, 5), TimeSpan.FromMinutes(90), new SheetWriteOptions()));
        Assert.False(CellWriter.WriteDate(ws.Cell(10, 6), TimeSpan.FromMinutes(5), new SheetWriteOptions { DateFormat = "[h]:mm" }));
        Assert.True(CellWriter.WriteDate(ws.Cell(10, 7), "=cmd", new SheetWriteOptions()));
        Assert.False(CellWriter.WriteNumber(ws.Cell(10, 8), 1.25m, new SheetWriteOptions { NumberFormat = "0.000" }));
        Assert.False(CellWriter.WriteNumber(ws.Cell(10, 9), 1.5f, new SheetWriteOptions()));
        Assert.False(CellWriter.WriteNumber(ws.Cell(10, 10), 2.5d, new SheetWriteOptions()));
        Assert.False(CellWriter.WriteInteger(ws.Cell(10, 11), 9, new SheetWriteOptions()));
        Assert.False(CellWriter.WriteInteger(ws.Cell(10, 12), ulong.MaxValue, new SheetWriteOptions()));
        Assert.False(CellWriter.WriteInteger(ws.Cell(10, 13), 3UL, new SheetWriteOptions { NumberFormat = "0" }));

        var dateCell = ws.Cell(20, 1);
        dateCell.Value = 45000;
        dateCell.Style.NumberFormat.Format = "yyyy-mm-dd";
        Assert.IsType<DateTime>(CellReader.Read(dateCell));
        dateCell.Style.NumberFormat.Format = "dd";
        _ = CellReader.Read(dateCell);
        dateCell.Style.NumberFormat.Format = "d-mmm";
        _ = CellReader.Read(dateCell);
        dateCell.Style.NumberFormat.Format = "[h]:mm:ss";
        var hours = CellReader.Read(dateCell);
        Assert.True(hours is double or DateTime or string);
        dateCell.Style.DateFormat.Format = "";
        dateCell.Style.NumberFormat.Format = "";
        Assert.IsType<double>(CellReader.Read(dateCell));
        ws.Cell(20, 2).Value = true;
        Assert.IsType<bool>(CellReader.Read(ws.Cell(20, 2)));
        ws.Cell(20, 3).Value = "text";
        Assert.IsType<string>(CellReader.Read(ws.Cell(20, 3)));
        ws.Cell(20, 4).Clear();
        Assert.Null(CellReader.Read(ws.Cell(20, 4)));
    }

    [Fact]
    public void Original_names_slash_dot_and_campaign_jsonl_array()
    {
        Assert.Equal("file", OriginalNames.Stem("."));
        if (OperatingSystem.IsWindows())
        {
            var slash = OriginalNames.Validate("/");
            Assert.False(string.IsNullOrWhiteSpace(slash));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => OriginalNames.Validate("/"));
        }

        var dir = Path.Combine(Path.GetTempPath(), "vest-hot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "camp.jsonl");
        CampaignJsonl.Append(path, new[] { 1, 2, 3 });
        CampaignJsonl.Append(path, new { kind = "echo", campaignId = "c1" });
        var rows = CampaignJsonl.Read(path);
        Assert.Contains(rows, r => r.ContainsKey("kind"));
        Assert.False(CampaignJsonl.HasTerminalWindow(rows, "c1", "2026-09-10", "08:00"));
        Assert.True(CampaignJsonl.HasKind(rows, "c1", "echo"));
    }

    [Fact]
    public void Csv_table_iterator_export_chars_and_codec_helpers()
    {
        IEnumerable<IReadOnlyList<object?>> Rows()
        {
            yield return ["a"];
            yield return ["b"];
        }

        var table = CsvTable.Create(["H"], Rows(), "iter");
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("iter", table.Name);
        var fromList = CsvTable.Create(["H"], new List<IReadOnlyList<object?>> { new object?[] { "z" } });
        Assert.Single(fromList.Rows);
        Assert.Throws<ArgumentException>(() => CsvTable.Create([], [["x"]]));
        Assert.Throws<ArgumentNullException>(() => CsvTable.Create(null!, [["x"]]));
        Assert.Throws<ArgumentNullException>(() => CsvTable.Create(["H"], null!));

        var named = CsvHelper.NewExportPath("Csv", "bad/name*?");
        Assert.DoesNotContain("/", Path.GetFileName(named));
        var wbNamed = WorkbookHelper.NewExportPath("ClosedXml", "bad/name*?");
        Assert.DoesNotContain("/", Path.GetFileName(wbNamed));

        var path = Path.Combine(Path.GetTempPath(), "vest-hot-" + Guid.NewGuid().ToString("N"), "bad.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "A\r\n\"unclosed");
        Assert.Throws<CsvFormatException>(() => CsvHelper.Open(path));

        Assert.Equal("9", CsvCodec.FormatInteger(9));
        Assert.Equal("1.5", CsvCodec.FormatFloating(1.5m));
        Assert.Equal((1.25f).ToString("G9", CultureInfo.InvariantCulture), CsvCodec.FormatFloating(1.25f));
        Assert.Equal((2.5d).ToString("G17", CultureInfo.InvariantCulture), CsvCodec.FormatFloating(2.5d));
        Assert.Equal("x", CsvCodec.FormatFloating("x"));
        var utc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        Assert.Equal("2026-01-02T03:04:05.000Z", CsvCodec.FormatDateTime(utc));
        Assert.Equal("2026-01-02T03:04:05.000", CsvCodec.FormatDateTime(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified)));
        Assert.Equal("01:00:00", CsvCodec.FormatDateTime(TimeSpan.FromHours(1)));
        Assert.Equal("ok", CsvCodec.FormatDateTime("ok"));

        var t0 = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        var series = NumericSeries.FromObservations(
        [
            new Observation(1m, t0),
            new Observation(2m, t0.AddSeconds(1)),
            new Observation(3m, t0.AddSeconds(2))
        ], "timed");
        var csvPath = Path.Combine(Path.GetDirectoryName(path)!, "series.csv");
        CsvHelper.WriteSeries(series, csvPath, new CsvOptions { Utf8Bom = false });
        var back = CsvHelper.Read(csvPath, new CsvOptions { Utf8Bom = false });
        Assert.False(string.IsNullOrEmpty(back.Rows[0][2] as string));

        var mixed = NumericSeries.FromObservations(
        [
            new Observation(1m, t0),
            new Observation(2m, null),
            new Observation(3m, t0.AddSeconds(2))
        ], "mixed");
        var mixedPath = Path.Combine(Path.GetDirectoryName(path)!, "mixed.csv");
        CsvHelper.WriteSeries(mixed, mixedPath, new CsvOptions { Utf8Bom = false });
        Assert.True(File.Exists(mixedPath));
    }

    [Fact]
    public void Fileio_collision_plan_and_encryption_length_headers()
    {
        var skip = FileIoJob.ResolveCollision(true, FileIoCollision.Skip, "/d/a.txt", "/d", "a.txt", ".##", []);
        Assert.True(skip.Skip);
        var overwrite = FileIoJob.ResolveCollision(true, FileIoCollision.Overwrite, "/d/a.txt", "/d", "a.txt", ".##", []);
        Assert.True(overwrite.Overwrite);
        var fresh = FileIoJob.ResolveCollision(false, FileIoCollision.Skip, "/d/a.txt", "/d", "a.txt", ".##", []);
        Assert.False(fresh.Skip);
        Assert.Equal("/d/a.txt", fresh.FinalPath);
        var unique = FileIoJob.ResolveCollision(true, FileIoCollision.UniqueName, "/d/a.txt", "/d", "a.txt", ".##", []);
        Assert.True(unique.Unique);
        Assert.Contains("a.", unique.FinalPath, StringComparison.Ordinal);
        var cap = FileIoJob.ResolveCollision(
            true,
            FileIoCollision.UniqueName,
            "/d/report.txt",
            "/d",
            "report.txt",
            ".#",
            ["report.1.txt", "report.2.txt", "report.3.txt", "report.4.txt", "report.5.txt", "report.6.txt", "report.7.txt", "report.8.txt", "report.9.txt"]);
        Assert.True(cap.Fail);

        using var seek = new MemoryStream(new byte[16]);
        seek.Position = 4;
        var fromSeek = EncryptionHelper.ResolvePlaintextLength(seek, null);
        Assert.False(fromSeek.Unknown);
        Assert.Equal(12, fromSeek.Length);
        var explicitLen = EncryptionHelper.ResolvePlaintextLength(seek, 9);
        Assert.Equal(9, explicitLen.Length);
        Assert.False(explicitLen.Unknown);
        var negative = EncryptionHelper.ResolvePlaintextLength(seek, -1);
        Assert.True(negative.Unknown);
        using var nonSeek = new ForwardOnlyStream(new byte[8]);
        var unknown = EncryptionHelper.ResolvePlaintextLength(nonSeek, null);
        Assert.True(unknown.Unknown);
        Assert.Equal(-1, unknown.Length);
        var knownNonSeek = EncryptionHelper.ResolvePlaintextLength(nonSeek, 3);
        Assert.False(knownNonSeek.Unknown);
        Assert.Equal(3, knownNonSeek.Length);
        Assert.Equal(0, EncryptionHelper.FrameCountFor(0));
        Assert.Equal(0, EncryptionHelper.FrameCountFor(-4));
        Assert.Equal(1, EncryptionHelper.FrameCountFor(1));

        var nonce = new byte[12];
        var salt = new byte[16];
        var header = new EnvelopeHeader
        {
            Alg = 1,
            Kdf = 0,
            FrameSize = Envelope.FrameSize,
            FileNonce = nonce,
            Salt = salt,
            FrameCount = 1
        };
        var trailer = new TrailerFields
        {
            Alg = 1,
            Kdf = 0,
            FrameSize = Envelope.FrameSize,
            FileNonce = nonce,
            Salt = salt,
            FrameCount = 1,
            PlaintextLength = Envelope.FrameSize
        };
        var problems = new List<string>();
        Assert.True(EncryptionHelper.HeadersAgree(header, trailer, problems));

        var algMismatch = new TrailerFields
        {
            Alg = 2,
            Kdf = 0,
            FrameSize = Envelope.FrameSize,
            FileNonce = nonce,
            Salt = salt,
            FrameCount = 1,
            PlaintextLength = Envelope.FrameSize
        };
        Assert.False(EncryptionHelper.HeadersAgree(header, algMismatch, []));

        var dirtySalt = new byte[16];
        dirtySalt[0] = 1;
        var saltTrailer = new TrailerFields
        {
            Alg = 1,
            Kdf = 0,
            FrameSize = Envelope.FrameSize,
            FileNonce = nonce,
            Salt = dirtySalt,
            FrameCount = 1,
            PlaintextLength = Envelope.FrameSize
        };
        Assert.False(EncryptionHelper.HeadersAgree(header, saltTrailer, []));

        var kdfHeader = new EnvelopeHeader
        {
            Alg = 1,
            Kdf = 1,
            FrameSize = Envelope.FrameSize,
            FileNonce = nonce,
            Salt = salt,
            FrameCount = 1
        };
        var kdfTrailer = new TrailerFields
        {
            Alg = 1,
            Kdf = 1,
            FrameSize = Envelope.FrameSize,
            FileNonce = nonce,
            Salt = dirtySalt,
            FrameCount = 1,
            PlaintextLength = Envelope.FrameSize
        };
        Assert.False(EncryptionHelper.HeadersAgree(kdfHeader, kdfTrailer, []));

        var zeroHeader = new EnvelopeHeader
        {
            Alg = 1,
            Kdf = 0,
            FrameSize = Envelope.FrameSize,
            FileNonce = nonce,
            Salt = salt,
            FrameCount = 0
        };
        Assert.False(EncryptionHelper.HeadersAgree(zeroHeader, trailer, []));

        using var keySecret = EncryptionSecret.FromKey(Key32());
        var key = keySecret.DeriveContentKey(0, new byte[16], 0, 0, 0);
        using var dest = new MemoryStream();
        var cipher = new byte[64];
        var tag = new byte[16];
        var fileNonce = new byte[12];
        var prefix = new byte[8];
        EncryptionHelper.WritePayloadFrame(dest, EncryptionAlgorithm.Aes256Gcm, key, fileNonce, prefix, 0, "hello"u8, cipher, tag);
        Assert.True(dest.Length > 5);
        EncryptionHelper.WritePayloadFrame(dest, EncryptionAlgorithm.Aes256CbcHmac, key, fileNonce, prefix, 1, "hello-world!!!!"u8, cipher, tag);
        Assert.True(dest.Length > 20);
    }

    [Fact]
    public void Workbook_open_corrupt_and_write_series_without_prefix()
    {
        var dir = Path.Combine(Path.GetTempPath(), "vest-hot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var bogus = Path.Combine(dir, "not.xlsx");
        File.WriteAllText(bogus, "not a workbook");
        Assert.ThrowsAny<Exception>(() => WorkbookHelper.Open(bogus));

        using var book = WorkbookHelper.Create("Summary", "ClosedXml");
        book.IncludeCharts = false;
        WorkbookHelper.WriteSeries(book, NumericSeries.From(new[] { 1, 2, 3, 4, 5 }, "seq"), prefix: null, populationSize: null);
        Assert.Contains("Summary", book.SheetNames);
        using var buffer = new MemoryStream();
        book.SaveTo(buffer);
        Assert.True(buffer.Length > 0);
    }

    static byte[] Key32()
    {
        var key = new byte[32];
        key[0] = 7;
        key[31] = 9;
        return key;
    }

    sealed class ForwardOnlyStream : Stream
    {
        readonly MemoryStream _inner;
        public ForwardOnlyStream(byte[] data) => _inner = new MemoryStream(data);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
