using System.Net;
using System.Net.Http.Headers;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPr01Tests : IDisposable
{
    private const string SampleMac = "00:1A:2B:3C:4D:5E";

    public void Dispose() => NetworkTestHooks.Reset();

    [Fact]
    public async Task PR01_001_custom_registry_http_is_rejected()
    {
        var options = new OuiLookupOptions
        {
            RegistryUrl = "http://api.macvendors.com/{oui}",
            AllowCustomRegistry = true,
            AllowedRegistryHosts = { "api.macvendors.com" },
            Handler = new StubOuiHandler(HttpStatusCode.OK, "Vendor")
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            NetworkHelper.LookupOuiAsync(SampleMac, options));
        Assert.Contains("HTTPS", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PR01_001_custom_registry_without_allow_is_rejected()
    {
        var options = new OuiLookupOptions
        {
            RegistryUrl = "https://oui.example.invalid/{oui}",
            Handler = new StubOuiHandler(HttpStatusCode.OK, "Vendor")
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            NetworkHelper.LookupOuiAsync(SampleMac, options));
        Assert.Contains("allowlisted", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, ((StubOuiHandler)options.Handler).Calls);
    }

    [Fact]
    public async Task PR01_001_loopback_https_is_rejected()
    {
        foreach (var url in new[]
        {
            "https://127.0.0.1/{oui}",
            "https://[::1]/{oui}",
            "https://localhost/{oui}"
        })
        {
            var options = new OuiLookupOptions
            {
                RegistryUrl = url,
                AllowCustomRegistry = true,
                AllowedRegistryHosts = { "127.0.0.1", "::1", "localhost" },
                Handler = new StubOuiHandler(HttpStatusCode.OK, "Vendor")
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                NetworkHelper.LookupOuiAsync(SampleMac, options));
            Assert.Contains("not allowed", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task PR01_001_redirect_is_not_followed()
    {
        var handler = new StubOuiHandler(HttpStatusCode.Found, "Vendor")
        {
            Location = "https://127.0.0.1/steal"
        };
        var options = new OuiLookupOptions
        {
            Handler = handler
        };

        var result = await NetworkHelper.LookupOuiAsync(SampleMac, options);
        Assert.Equal(OuiSource.None, result.Source);
        Assert.Null(result.Vendor);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public void PR01_001_parse_mac_does_not_http()
    {
        var handler = new StubOuiHandler(HttpStatusCode.OK, "Vendor");
        _ = NetworkHelper.ParseMac(SampleMac);
        Assert.Equal(0, handler.Calls);
        Assert.Equal("00:1A:2B:3C:4D:5E", NetworkHelper.ParseMac(SampleMac).Colon);
    }

    [Fact]
    public async Task PR01_001_default_host_with_stub_is_live()
    {
        var handler = new StubOuiHandler(HttpStatusCode.OK, "Cisco Systems, Inc");
        var result = await NetworkHelper.LookupOuiAsync(SampleMac, new OuiLookupOptions { Handler = handler });
        Assert.Equal(OuiSource.Live, result.Source);
        Assert.Equal("Cisco Systems, Inc", result.Vendor);
        Assert.Equal(1, handler.Calls);
        Assert.StartsWith("https://api.macvendors.com/", handler.LastUri?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PR01_001_rfc1918_https_is_rejected()
    {
        var options = new OuiLookupOptions
        {
            RegistryUrl = "https://192.168.1.20/{oui}",
            AllowCustomRegistry = true,
            AllowedRegistryHosts = { "192.168.1.20" },
            Handler = new StubOuiHandler(HttpStatusCode.OK, "Vendor")
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            NetworkHelper.LookupOuiAsync(SampleMac, options));
    }

    [Fact]
    public void PR01_002_hooks_are_not_public()
    {
        var type = typeof(NetworkHelper).Assembly.GetType("Vestigium.Helpers.Network.NetworkTestHooks");
        Assert.NotNull(type);
        Assert.False(type!.IsPublic);
        Assert.True(type.IsNotPublic);
    }

    [Fact]
    public void PR01_002_reset_clears_injected_state()
    {
        NetworkTestHooks.CampaignRoot = Path.GetTempPath();
        NetworkTestHooks.UtcNow = DateTimeOffset.UnixEpoch;
        NetworkTestHooks.ProcRoot = "/tmp";
        NetworkTestHooks.Reset();
        Assert.Null(NetworkTestHooks.CampaignRoot);
        Assert.Null(NetworkTestHooks.UtcNow);
        Assert.Null(NetworkTestHooks.ProcRoot);
    }

    [Fact]
    public void PR01_003_continuous_without_duration_gets_default_cap()
    {
        var options = new IcmpEchoOptions { Count = 0 };
        _ = NetworkHelper.IcmpEcho("127.0.0.1", options);
        Assert.Equal(IcmpEchoOptions.DefaultContinuousDuration, options.MaxDuration);
    }

    [Fact]
    public void PR01_003_zero_interval_rejected_without_burst()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions
            {
                Count = 0,
                Interval = TimeSpan.Zero,
                MaxDuration = TimeSpan.FromSeconds(30)
            }));
        Assert.Contains("200 ms", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PR01_003_finite_count_may_use_zero_interval()
    {
        var job = NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions
        {
            Count = 4,
            Interval = TimeSpan.Zero
        });
        Assert.NotNull(job);
    }

    [Fact]
    public void PR01_003_long_job_requires_one_second_interval()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions
            {
                Count = 0,
                Interval = TimeSpan.FromMilliseconds(200),
                MaxDuration = TimeSpan.FromHours(1)
            }));
        Assert.Contains("1 second", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PR01_003_burst_past_one_minute_is_rejected()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions
            {
                Count = 0,
                AllowBurst = true,
                Interval = TimeSpan.FromMilliseconds(20),
                MaxDuration = TimeSpan.FromHours(1)
            }));
        Assert.Contains("one minute", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PR01_003_duration_over_24h_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions
            {
                Count = 0,
                MaxDuration = TimeSpan.FromHours(25),
                Interval = TimeSpan.FromSeconds(1)
            }));
    }

    [Fact]
    public void PR01_003_short_burst_is_allowed()
    {
        var job = NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions
        {
            Count = 0,
            AllowBurst = true,
            Interval = TimeSpan.Zero,
            MaxDuration = TimeSpan.FromSeconds(30)
        });
        Assert.NotNull(job);
    }

    [Fact]
    public void PR01_003_one_hour_at_one_second_is_allowed()
    {
        var job = NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions
        {
            Count = 0,
            Interval = TimeSpan.FromSeconds(1),
            MaxDuration = TimeSpan.FromHours(1)
        });
        Assert.NotNull(job);
    }

    [Fact]
    public void PR01_004_udp_foreign_source_discarded()
    {
        var server = IPAddress.Parse("1.1.1.1");
        Assert.True(DnsClient.IsExpectedDnsPeer(new IPEndPoint(server, 53), server, 53));
        Assert.True(DnsClient.IsExpectedDnsPeer(new IPEndPoint(IPAddress.Parse("::ffff:1.1.1.1"), 53), server, 53));
        Assert.False(DnsClient.IsExpectedDnsPeer(new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53), server, 53));
        Assert.False(DnsClient.IsExpectedDnsPeer(new IPEndPoint(server, 5353), server, 53));
        Assert.False(DnsClient.IsExpectedDnsPeer(new DnsEndPoint("1.1.1.1", 53), server, 53));
        Assert.False(DnsClient.IsExpectedDnsPeer(null, server, 53));
    }

    private sealed class StubOuiHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? Location { get; init; }
        public int Calls { get; private set; }
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastUri = request.RequestUri;
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body)
            };
            if (!string.IsNullOrWhiteSpace(Location))
                response.Headers.Location = new Uri(Location, UriKind.Absolute);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            return Task.FromResult(response);
        }
    }
}
