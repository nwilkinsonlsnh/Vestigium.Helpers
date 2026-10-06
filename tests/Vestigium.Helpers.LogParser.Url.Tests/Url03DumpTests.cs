using Vestigium.Helpers.LogParser;
using Vestigium.Helpers.LogParser.Url;

namespace Vestigium.Helpers.LogParser.Url.Tests;

public sealed class Url03DumpTests
{
    [Fact]
    public void URL03_001_email_dump_file_keeps_three_names_and_drops_the_filename()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "email-dump.txt");
        var result = UrlReader.ReadFile(path);
        var hosts = result.Hosts.Select(h => h.Host).ToArray();

        Assert.Contains("q2prod.idbs-cloud.com", hosts);
        Assert.Contains("q2valprod.services.idbs-cloud.com", hosts);
        Assert.Contains("login.microsoftonline.com", hosts);
        Assert.DoesNotContain(hosts, h => h is "notes.txt" or "e.g");

        Assert.Equal([8443], result.Hosts.Single(h => h.Host == "q2prod.idbs-cloud.com").Ports);
        Assert.Contains(result.Hosts, h => h.Host == "75.2.119.14" && h.IsAddress);
        Assert.Contains(result.Hosts, h => h.Host == "localhost");
        Assert.Equal(LogFormat.Url, result.Format);
    }
}
