namespace Vestigium.Helpers.Network;

public enum TlsProbeStatus
{
    Handshook,
    Refused,
    TimedOut,
    Error
}

public sealed class TlsProbeOptions
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public TimeSpan Timeout { get; set; } = DefaultTimeout;
    public int InterfaceIndex { get; set; }
    public string? SourceAddress { get; set; }
    public RouteFamily Family { get; set; } = RouteFamily.All;
    public string? ServerName { get; set; }
}

public sealed record TlsProbeResult(
    string JobId,
    string Host,
    int Port,
    TlsProbeStatus Status,
    string? Subject,
    string? Issuer,
    DateTimeOffset? NotAfter,
    string? Protocol,
    long ElapsedMs,
    string? Detail);
