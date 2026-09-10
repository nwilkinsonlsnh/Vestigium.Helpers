namespace Vestigium.Helpers.Network;

public enum EuiKind
{
    Eui48 = 48,
    Eui64 = 64
}

public enum MacFormat
{
    Colon = 0,
    Hyphen = 1,
    Cisco = 2,
    Bare = 3,
    Integer = 4
}

public enum OuiSource
{
    None = 0,
    Live = 1,
    File = 2
}

public sealed record MacAddress(
    EuiKind Kind,
    IReadOnlyList<byte> Octets,
    string Colon,
    string Hyphen,
    string Cisco,
    string Bare,
    ulong Integer,
    bool IsMulticast,
    bool IsLocallyAdministered,
    bool IsBroadcast,
    bool IsUnspecified,
    string Oui24,
    string? ModifiedEui64,
    string? LinkLocal);

public sealed class OuiLookupOptions
{
    public static readonly string DefaultRegistryUrl = "https://api.macvendors.com/{oui}";
    public static readonly string Disclaimer =
        "IEEE / registry assignment at query time. Not proof of current hardware owner. Requires outbound HTTPS. May be stale or rate-limited.";

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);
    public string RegistryUrl { get; set; } = DefaultRegistryUrl;
    public string? RegistryFilePath { get; set; }
    public HttpMessageHandler? Handler { get; set; }
}

public sealed record OuiLookupResult(
    string Query,
    string? Vendor,
    OuiSource Source,
    string Disclaimer);
