using System.Net;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class DnsLookupTests
{
    [Fact]
    public async Task Lookup_localhost_returns_loopback()
    {
        var result = await NetworkHelper.LookupAsync("localhost");
        Assert.Contains(
            result.Answers,
            a => IPAddress.TryParse(a.Data, out var ip) && IPAddress.IsLoopback(ip));
    }

    [Fact]
    public async Task Lookup_explicit_dead_server_does_not_throw()
    {
        var result = await NetworkHelper.LookupAsync(
            "example.invalid",
            new DnsLookupOptions
            {
                Server = "127.0.0.1",
                Port = 1,
                Type = DnsRecordType.A,
                Timeout = TimeSpan.FromMilliseconds(400)
            });
        Assert.True(
            result.Rcode is DnsRcode.Timeout or DnsRcode.Refused or DnsRcode.Failed or DnsRcode.ServFail,
            result.Rcode.ToString());
        Assert.Equal("127.0.0.1", result.Server);
    }

    [Fact]
    public async Task LookupMany_localhost_has_one_row()
    {
        var rows = await NetworkHelper.LookupManyAsync(["localhost"]);
        Assert.Single(rows);
    }

    [Fact]
    public async Task Blank_name_throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => NetworkHelper.LookupAsync(" "));
    }
}
