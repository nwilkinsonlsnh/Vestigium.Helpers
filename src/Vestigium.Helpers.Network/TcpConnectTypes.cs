namespace Vestigium.Helpers.Network;

public enum TcpConnectStatus
{
    Connected,
    Refused,
    TimedOut
}

public sealed class TcpConnectOptions
{
    public const int DefaultBannerMaxBytes = 256;
    public static readonly TimeSpan DefaultBannerTimeout = TimeSpan.FromMilliseconds(500);

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(4);
    public int InterfaceIndex { get; set; }
    public string? SourceAddress { get; set; }
    public RouteFamily Family { get; set; } = RouteFamily.All;
    public bool ReadBanner { get; set; }
    public int BannerMaxBytes { get; set; } = DefaultBannerMaxBytes;
    public TimeSpan BannerTimeout { get; set; } = DefaultBannerTimeout;
}

public sealed record TcpConnectResult(
    string JobId,
    string Host,
    int Port,
    string? Address,
    TcpConnectStatus Status,
    long ElapsedMs,
    string? Detail,
    int? Mss = null,
    string? Banner = null);
