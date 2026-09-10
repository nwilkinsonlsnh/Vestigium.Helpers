using System.Net;
using System.Net.Http;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkBranchSweepTests : IDisposable
{
    readonly string _root = Path.Combine(Path.GetTempPath(), "vest-sweep-" + Guid.NewGuid().ToString("N"));

    public NetworkBranchSweepTests()
    {
        Directory.CreateDirectory(_root);
        NetworkTestHooks.CampaignRoot = _root;
        NetworkTestHooks.UtcNow = new DateTimeOffset(2026, 9, 10, 12, 5, 0, TimeSpan.Zero);
    }

    public void Dispose()
    {
        NetworkTestHooks.Reset();
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void Probe_and_aliases_are_stable()
    {
        Assert.Equal("Vestigium.Helpers.Network", NetworkHelper.Probe());
        Assert.NotEmpty(NetworkHelper.CommonBots);
        Assert.Equal(86400, NetworkHelper.BandwidthSeconds(BandwidthBasis.Day));
    }

    [Fact]
    public void Mac_parses_hex_decimal_ipv6_and_formats()
    {
        var colon = NetworkHelper.ParseMac("00:1A:2B:3C:4D:5E");
        Assert.Equal(colon.Colon, NetworkHelper.FormatMac(colon, MacFormat.Colon));
        Assert.Equal(colon.Hyphen, NetworkHelper.FormatMac(colon, MacFormat.Hyphen));
        Assert.Equal(colon.Cisco, NetworkHelper.FormatMac(colon, MacFormat.Cisco));
        Assert.Equal(colon.Bare, NetworkHelper.FormatMac(colon, MacFormat.Bare));
        Assert.Equal(colon.Integer.ToString(), NetworkHelper.FormatMac(colon, MacFormat.Integer));
        Assert.Equal(colon.Colon, NetworkHelper.FormatMac(colon, (MacFormat)99));

        var hex = NetworkHelper.ParseMac("0x001A2B3C4D5E");
        Assert.Equal(colon.Colon, hex.Colon);
        var dec = NetworkHelper.ParseMac(colon.Integer.ToString());
        Assert.Equal(colon.Colon, dec.Colon);
        var spaced = NetworkHelper.ParseMac("00 1A 2B 3C 4D 5E");
        Assert.Equal(colon.Colon, spaced.Colon);

        var eui64 = NetworkHelper.ParseMac("02:1A:2B:FF:FE:3C:4D:5E");
        Assert.Equal(EuiKind.Eui64, eui64.Kind);
        Assert.Equal(eui64.Colon, NetworkHelper.ToModifiedEui64(eui64).Colon);
        Assert.StartsWith("fe80:", eui64.LinkLocal, StringComparison.OrdinalIgnoreCase);

        var zero = NetworkHelper.ParseMac("00-00-00-00-00-00");
        Assert.True(zero.IsUnspecified);
        var local = NetworkHelper.ParseMac("02:00:00:00:00:01");
        Assert.True(local.IsLocallyAdministered);

        var v6 = NetworkHelper.ParseMac("fe80::1a:2bff:fe3c:4d5e");
        Assert.Equal(EuiKind.Eui64, v6.Kind);

        Assert.Throws<ArgumentException>(() => NetworkHelper.ParseMac("00:1A:2B"));
        Assert.Throws<ArgumentException>(() => NetworkHelper.ParseMac("0xzz"));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.MacFromInteger(1UL << 48));
        var wide = NetworkHelper.MacFromInteger(1UL << 48, EuiKind.Eui64);
        Assert.Equal(EuiKind.Eui64, wide.Kind);
    }

    [Fact]
    public async Task Oui_live_handler_success_html_empty_and_404()
    {
        var ok = await NetworkHelper.LookupOuiAsync(
            "00:1A:2B:3C:4D:5E",
            new OuiLookupOptions { Handler = new StubHandler(Ok("Acme Instruments")), Timeout = TimeSpan.Zero });
        Assert.Equal("Acme Instruments", ok.Vendor);
        Assert.Equal(OuiSource.Live, ok.Source);

        var longVendor = new string('V', 220);
        var clipped = await NetworkHelper.LookupOuiAsync(
            "00:1A:2B:3C:4D:5E",
            new OuiLookupOptions { Handler = new StubHandler(Ok(longVendor)) });
        Assert.Equal(200, clipped.Vendor!.Length);

        var html = await NetworkHelper.LookupOuiAsync(
            "00:1A:2B:3C:4D:5E",
            new OuiLookupOptions { Handler = new StubHandler(Ok("<html>nope</html>")) });
        Assert.Null(html.Vendor);

        var empty = await NetworkHelper.LookupOuiAsync(
            "00:1A:2B:3C:4D:5E",
            new OuiLookupOptions { Handler = new StubHandler(Ok("")) });
        Assert.Null(empty.Vendor);

        var missing = await NetworkHelper.LookupOuiAsync(
            "00:1A:2B:3C:4D:5E",
            new OuiLookupOptions { Handler = new StubHandler(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)) });
        Assert.Equal(OuiSource.None, missing.Source);

        var boom = await NetworkHelper.LookupOuiAsync(
            "00:1A:2B:3C:4D:5E",
            new OuiLookupOptions { Handler = new ThrowingHandler(), RegistryUrl = "http://example.test/{mac}" });
        Assert.Null(boom.Vendor);
    }

    [Fact]
    public void Oui_registry_file_edges()
    {
        var path = Path.Combine(_root, "oui.txt");
        File.WriteAllText(path,
            "# comment\n; also\n\n00:11:22,VendorA\nnot-a-row\n{\"vendor\":\"NoOui\"}\n{\"oui\":\"AA:BB:CC\"}\n{\"oui\":\"DD-EE-FF\",\"vendor\":\"JsonCo\"}\n11-22-33|PipeCo\n");
        var map = NetworkHelper.LoadOuiRegistry(path);
        Assert.Equal("VendorA", map["00:11:22"]);
        Assert.Equal("JsonCo", map["DD:EE:FF"]);
        Assert.Equal("PipeCo", map["11:22:33"]);
        Assert.Throws<FileNotFoundException>(() => NetworkHelper.LoadOuiRegistry(Path.Combine(_root, "missing.txt")));
        Assert.Equal("0011", OuiRegistry.Normalize("0011"));
        Assert.Equal("00:11:22", OuiRegistry.Normalize("00112233"));
    }

    [Fact]
    public void Percentile_bill_guards()
    {
        Assert.Throws<ArgumentNullException>(() => NetworkHelper.BillP95((IEnumerable<decimal>)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.BillPercentile([1m], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.BillPercentile([1m], 1));
        var mid = NetworkHelper.BillPercentile([10m, 20m, 30m], 0.5);
        Assert.Equal(0.5, mid.Percentile);
    }

    [Fact]
    public void Route_linux_is_typed_deny()
    {
        var change = new NetworkRouteChange
        {
            Destination = "192.168.9.0",
            Gateway = "192.168.9.1",
            PrefixLength = 24,
            Persistent = true,
            Metric = 0
        };
        if (!OperatingSystem.IsWindows())
        {
            Assert.Throws<PlatformNotSupportedException>(() => NetworkHelper.AddRoute(change));
            Assert.Throws<PlatformNotSupportedException>(() => NetworkHelper.ChangeRoute(change));
            Assert.Throws<PlatformNotSupportedException>(() => NetworkHelper.RemoveRoute(change));
            Assert.Throws<PlatformNotSupportedException>(() => NetworkHelper.DeleteRoute(change));
        }
        Assert.Throws<ArgumentException>(() => NetworkHelper.AddRoute(new NetworkRouteChange
        {
            Destination = "not-ip",
            Gateway = "192.168.1.1",
            PrefixLength = 24
        }));
    }

    [Fact]
    public async Task Campaign_guards_open_outside_range_and_end()
    {
        Assert.Throws<ArgumentNullException>(() => NetworkHelper.CreateEchoCampaign(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            RangeStartDate = new DateOnly(2026, 9, 11),
            RangeEndDate = new DateOnly(2026, 9, 10),
            Windows = [new EchoWindow(new TimeOnly(12, 0), 1)]
        }));
        Assert.Throws<ArgumentException>(() => NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            Windows = []
        }));
        Assert.Throws<ArgumentException>(() => NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            Windows = [new EchoWindow(new TimeOnly(12, 0), 1), new EchoWindow(new TimeOnly(12, 0), 2)]
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            Windows = [new EchoWindow(new TimeOnly(12, 0), 0)]
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            Grace = TimeSpan.FromHours(-1),
            Windows = [new EchoWindow(new TimeOnly(12, 0), 1)]
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            Grace = TimeSpan.FromHours(13),
            Windows = [new EchoWindow(new TimeOnly(12, 0), 1)]
        }));

        var recipe = Path.Combine(_root, "recipe.json");
        var created = NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            RangeStartDate = new DateOnly(2026, 9, 10),
            RangeEndDate = new DateOnly(2026, 9, 10),
            TimeZoneId = "UTC",
            RecipePath = recipe,
            Grace = TimeSpan.FromMinutes(15),
            Echo = new IcmpEchoOptions { Timeout = TimeSpan.FromMilliseconds(200), Interval = TimeSpan.Zero },
            Windows = [new EchoWindow(new TimeOnly(12, 0), 1)]
        });
        Assert.True(File.Exists(recipe));
        var opened = NetworkHelper.OpenEchoCampaign(recipe);
        Assert.Equal(created.CampaignId, opened.CampaignId);

        var outside = NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            RangeStartDate = new DateOnly(2026, 10, 1),
            RangeEndDate = new DateOnly(2026, 10, 2),
            TimeZoneId = null,
            Windows = [new EchoWindow(new TimeOnly(12, 0), 1)]
        });
        var skipped = await outside.RunAsync();
        Assert.Equal(0, skipped.WindowsRun);

        var ended = await created.RunAsync();
        Assert.True(ended.WindowsRun + ended.WindowsMissed + ended.WindowsSkipped >= 1);
        var jsonl = Directory.GetFiles(_root, "*.jsonl");
        Assert.NotEmpty(jsonl);
        var rows = CampaignJsonl.Read(jsonl[0]);
        Assert.False(CampaignJsonl.HasKind(rows, "no-such", "campaignStart"));
        Assert.False(CampaignJsonl.HasTerminalWindow(rows, created.CampaignId, "1999-01-01", "00:00"));
        CampaignJsonl.Append(Path.Combine(_root, "nl.jsonl"), new { kind = "note", text = "line1\nline2" });
        Assert.Empty(CampaignJsonl.Read(Path.Combine(_root, "missing.jsonl")));
    }

    [Fact]
    public void Echo_remaining_guards()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { Timeout = TimeSpan.FromMilliseconds(5) }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { BufferSize = 70_000 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { Ttl = 256 }));
        _ = NetworkHelper.IcmpEcho("127.0.0.1", null);
        _ = NetworkHelper.Ping("127.0.0.1");
    }

    sealed class StubHandler : HttpMessageHandler
    {
        readonly HttpResponseMessage _response;
        public StubHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_response);
    }

    sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("boom");
    }

    static HttpResponseMessage Ok(string body)
        => new(System.Net.HttpStatusCode.OK) { Content = new StringContent(body) };
}
