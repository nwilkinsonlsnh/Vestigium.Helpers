using Vestigium.Helpers.LogParser;

namespace Vestigium.Helpers.Tests;

public sealed class LogParserLp01Tests
{
    [Fact]
    public void LP01_001_format_is_har_and_url_only()
    {
        var names = Enum.GetNames<LogFormat>();
        Assert.Equal(["Unknown", "Har", "Url"], names);
        Assert.DoesNotContain(names, n => n == "Domain");
    }

    [Fact]
    public void LP01_002_host_folds_and_strips_one_trailing_dot()
    {
        var host = new LogHost("  Q2Prod.IDBS-Cloud.com. ", [8443, 443, 8443], 2, LogHostSource.Request | LogHostSource.Page, false);

        Assert.Equal("q2prod.idbs-cloud.com", host.Host);
        Assert.Equal([443, 8443], host.Ports.OrderBy(p => p));
        Assert.Equal(2, host.HitCount);
        Assert.True(host.Sources.HasFlag(LogHostSource.Request));
        Assert.False(host.IsAddress);
    }

    [Fact]
    public void LP01_003_address_literal_is_flagged_and_empty_host_throws()
    {
        var literal = new LogHost("75.2.119.14", null, 0, LogHostSource.Request, true);
        Assert.True(literal.IsAddress);
        Assert.Empty(literal.Ports);
        Assert.Throws<ArgumentException>(() => new LogHost("   ", null, 0, LogHostSource.None, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LogHost("a.example", [0], 0, LogHostSource.Url, false));
    }

    [Fact]
    public void LP01_004_read_result_freezes_rows()
    {
        var row = new LogHost("login.microsoftonline.com", null, 1, LogHostSource.Url, false);
        var result = new LogReadResult(LogFormat.Url, 1, 0, [row], ["log.version is not 1.2"]);

        Assert.Equal(LogFormat.Url, result.Format);
        Assert.Single(result.Hosts);
        Assert.Single(result.Warnings);
        Assert.True(((IList<LogHost>)result.Hosts).IsReadOnly);
    }
}
