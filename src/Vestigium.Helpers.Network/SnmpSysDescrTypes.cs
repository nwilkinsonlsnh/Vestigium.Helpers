namespace Vestigium.Helpers.Network;

public enum SnmpSysDescrStatus
{
    Answered,
    TimedOut,
    Error
}

public sealed class SnmpSysDescrOptions
{
    public const int DefaultPort = 161;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
    public int Port { get; set; } = DefaultPort;
    public string Community { get; set; } = "public";
    public int InterfaceIndex { get; set; }
    public string? SourceAddress { get; set; }
    public RouteFamily Family { get; set; } = RouteFamily.All;
}

public sealed record SnmpSysDescrResult(
    string JobId,
    string Host,
    string? Address,
    SnmpSysDescrStatus Status,
    string? SysDescr,
    long ElapsedMs,
    string? Detail);
