namespace Vestigium.Helpers.Network;

public enum TcpConnectStatus
{
    Connected,
    Refused,
    TimedOut
}

public sealed class TcpConnectOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(4);
    public int InterfaceIndex { get; set; }
    public string? SourceAddress { get; set; }
    public RouteFamily Family { get; set; } = RouteFamily.All;
}

public sealed record TcpConnectResult(
    string JobId,
    string Host,
    int Port,
    string? Address,
    TcpConnectStatus Status,
    long ElapsedMs,
    string? Detail);
