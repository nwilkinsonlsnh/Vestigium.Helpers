using System.Net;
using System.Net.Http.Headers;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR01Tests : IDisposable
{
    const string SampleMac = "00:1A:2B:3C:4D:5E";

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

    sealed class StubOuiHandler : HttpMessageHandler
    {
        readonly HttpStatusCode _status;
        readonly string _body;

        public StubOuiHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public string? Location { get; set; }
        public int Calls { get; private set; }
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastUri = request.RequestUri;
            var response = new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body)
            };
            if (!string.IsNullOrWhiteSpace(Location))
                response.Headers.Location = new Uri(Location, UriKind.Absolute);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            return Task.FromResult(response);
        }
    }
}
