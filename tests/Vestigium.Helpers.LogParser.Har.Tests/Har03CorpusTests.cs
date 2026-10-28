using Vestigium.Helpers.LogParser;
using Vestigium.Helpers.LogParser.Har;

namespace Vestigium.Helpers.LogParser.Har.Tests;

public sealed class Har03CorpusTests
{
    private static readonly string FixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    [Fact]
    public void HAR03_001_cannot_reach_keeps_the_timed_out_host_and_port()
    {
        var result = HarReader.ReadFile(Path.Combine(FixtureDir, "cannot-reach.har"));
        var hosts = result.Hosts.Select(h => h.Host).OrderBy(h => h).ToArray();

        Assert.Equal(
            ["q2prod.idbs-cloud.com", "q2valprod.services.idbs-cloud.com", "quintiles.sharepoint.com"],
            hosts);

        var prod = result.Hosts.Single(h => h.Host == "q2prod.idbs-cloud.com");
        Assert.Equal([8443], prod.Ports);
        Assert.Contains(result.Hosts, h => h.Host == "q2valprod.services.idbs-cloud.com" && h.HitCount > 0);
        Assert.DoesNotContain(result.Hosts, h => h.IsAddress);
    }

    [Fact]
    public void HAR03_002_sso_field_map_is_ten_hosts_and_no_server_ip()
    {
        var result = HarReader.ReadFile(Path.Combine(FixtureDir, "sso.har"));
        var hosts = result.Hosts.Select(h => h.Host).OrderBy(h => h).ToArray();

        Assert.Equal(
            [
                "aadcdn.msauth.net",
                "aadcdn.msftauth.net",
                "cdn.auth0.com",
                "idbs-q2valprod.us.auth0.com",
                "idbs-themes.idbs-cloud.com",
                "login.microsoftonline.com",
                "q2prod.idbs-cloud.com",
                "q2valprod.services.idbs-cloud.com",
                "quintiles.sharepoint.com",
                "static-resources.idbs-cloud.com",
            ],
            hosts);

        Assert.Equal([8443], result.Hosts.Single(h => h.Host == "q2prod.idbs-cloud.com").Ports);
        Assert.DoesNotContain(result.Hosts, h => h.Host is "75.2.119.14" or "13.107.136.2" or "login.windows.net");
    }

    [Fact]
    public void HAR03_003_data_uri_is_dropped_and_missing_entries_throws()
    {
        var data = HarReader.ReadFile(Path.Combine(FixtureDir, "data-uri.har"));
        Assert.Equal(["q2prod.idbs-cloud.com"], data.Hosts.Select(h => h.Host));

        var ex = Assert.Throws<InvalidDataException>(() => HarReader.ReadFile(Path.Combine(FixtureDir, "missing-entries.har")));
        Assert.Equal("HAR is missing log.entries.", ex.Message);
    }

    [Fact]
    public void HAR03_004_oversize_stream_is_rejected()
    {
        var ex = Assert.Throws<InvalidDataException>(() => HarReader.Read(new OversizeStream()));
        Assert.Equal("HAR file is over 64 MB.", ex.Message);
    }

    private sealed class OversizeStream : MemoryStream
    {
        public override bool CanSeek => true;
        public override long Length => HarReader.MaxBytes + 1;
    }
}
