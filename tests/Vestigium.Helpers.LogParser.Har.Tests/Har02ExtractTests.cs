using System.Text;
using Vestigium.Helpers.LogParser;
using Vestigium.Helpers.LogParser.Har;

namespace Vestigium.Helpers.LogParser.Har.Tests;

public sealed class Har02ExtractTests
{
    [Fact]
    public void HAR02_001_request_redirect_location_and_title_merge()
    {
        const string json = """
            {
              "log": {
                "version": "1.2",
                "pages": [ { "title": "https://q2prod.idbs-cloud.com:8443/login" } ],
                "entries": [
                  {
                    "request": { "url": "https://Q2Prod.IDBS-Cloud.com.:8443/a" },
                    "response": {
                      "status": 0,
                      "redirectURL": "https://q2valprod.services.idbs-cloud.com/auth",
                      "headers": [ { "name": "Location", "value": "https://login.microsoftonline.com/x" } ],
                      "serverIPAddress": "75.2.119.14"
                    }
                  },
                  {
                    "request": { "url": "data:text/plain,hi" },
                    "response": { "status": 0, "headers": [ { "name": "Location", "value": "/relative" } ] }
                  }
                ]
              }
            }
            """;

        var result = HarReader.Read(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        Assert.Equal(3, result.Hosts.Count);

        var prod = result.Hosts.Single(h => h.Host == "q2prod.idbs-cloud.com");
        Assert.Equal(1, prod.HitCount);
        Assert.Equal([8443], prod.Ports);
        Assert.True(prod.Sources.HasFlag(LogHostSource.Request));
        Assert.True(prod.Sources.HasFlag(LogHostSource.Page));
        Assert.False(prod.IsAddress);

        var auth = result.Hosts.Single(h => h.Host == "q2valprod.services.idbs-cloud.com");
        Assert.Equal(0, auth.HitCount);
        Assert.True(auth.Sources.HasFlag(LogHostSource.Redirect));

        Assert.Contains(result.Hosts, h => h.Host == "login.microsoftonline.com" && h.Sources.HasFlag(LogHostSource.Location));
        Assert.DoesNotContain(result.Hosts, h => h.Host == "75.2.119.14");
    }

    [Fact]
    public void HAR02_002_ip_literal_is_address_and_missing_entries_throws()
    {
        var literal = HarReader.Read(new MemoryStream(Encoding.UTF8.GetBytes(
            """{"log":{"version":"1.2","entries":[{"request":{"url":"http://75.2.119.14/x"}}]}}""")));
        Assert.True(literal.Hosts.Single().IsAddress);

        var ex = Assert.Throws<InvalidDataException>(() => HarReader.Read(new MemoryStream(Encoding.UTF8.GetBytes("{\"log\":{}}"))));
        Assert.Equal("HAR is missing log.entries.", ex.Message);
    }

    [Fact]
    public void HAR02_003_entry_error_stays_on_the_host()
    {
        const string json = """
            {"log":{"version":"1.2","entries":[{"_error":"net::ERR_TIMED_OUT","request":{"url":"https://q2valprod.services.idbs-cloud.com/a"},"response":{"status":0}}]}}
            """;
        var result = HarReader.Read(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        Assert.Equal("net::ERR_TIMED_OUT", result.Hosts.Single().Error);
    }
}
