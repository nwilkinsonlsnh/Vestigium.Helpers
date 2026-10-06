using System.Text;
using Vestigium.Helpers.LogParser;
using Vestigium.Helpers.LogParser.Url;

namespace Vestigium.Helpers.LogParser.Url.Tests;

public sealed class Url02ExtractTests
{
    [Fact]
    public void URL02_001_scheme_mailto_and_bare_name()
    {
        const string text = """
            See https://q2prod.idbs-cloud.com:8443/login
            Contact user@q2valprod.services.idbs-cloud.com
            Also login.microsoftonline.com and notes.txt and e.g. the rest.
            """;

        var result = UrlReader.Read(new MemoryStream(Encoding.UTF8.GetBytes(text)));
        Assert.Equal(3, result.Hosts.Count);

        var prod = result.Hosts.Single(h => h.Host == "q2prod.idbs-cloud.com");
        Assert.Equal([8443], prod.Ports);
        Assert.Equal(LogHostSource.Url, prod.Sources);

        Assert.Contains(result.Hosts, h => h.Host == "q2valprod.services.idbs-cloud.com");
        Assert.Contains(result.Hosts, h => h.Host == "login.microsoftonline.com");
        Assert.DoesNotContain(result.Hosts, h => h.Host is "notes.txt" or "e.g");
    }

    [Fact]
    public void URL02_002_address_and_localhost()
    {
        var result = UrlReader.Read(new MemoryStream(Encoding.UTF8.GetBytes("probe 75.2.119.14 and localhost")));
        Assert.Contains(result.Hosts, h => h.Host == "75.2.119.14" && h.IsAddress);
        Assert.Contains(result.Hosts, h => h.Host == "localhost" && !h.IsAddress);
    }

    [Fact]
    public void URL02_003_oversize_still_rejected()
    {
        var ex = Assert.Throws<InvalidDataException>(() => UrlReader.Read(new OversizeStream()));
        Assert.Equal("Text file is over 64 MB.", ex.Message);
    }

    private sealed class OversizeStream : MemoryStream
    {
        public override bool CanSeek => true;
        public override long Length => UrlReader.MaxBytes + 1;
    }
}
