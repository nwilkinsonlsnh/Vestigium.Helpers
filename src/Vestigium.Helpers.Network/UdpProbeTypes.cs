namespace Vestigium.Helpers.Network;

public enum UdpProbeStatus
{
    Replied,
    Unreachable,
    TimedOut
}

public sealed class UdpProbeOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(4);
    public int InterfaceIndex { get; set; }
    public string? SourceAddress { get; set; }
    public RouteFamily Family { get; set; } = RouteFamily.All;
    public int PayloadSize { get; set; } = 32;
}

public sealed record UdpProbeResult(
    string JobId,
    string Host,
    int Port,
    string? Address,
    UdpProbeStatus Status,
    long ElapsedMs,
    string? Detail);
