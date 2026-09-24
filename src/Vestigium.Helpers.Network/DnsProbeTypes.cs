namespace Vestigium.Helpers.Network;

public enum DnsProbeStatus
{
    Answered,
    Refused,
    TimedOut
}

public sealed record DnsProbeResult(
    string JobId,
    string Name,
    string? Server,
    DnsProbeStatus Status,
    DnsRcode Rcode,
    long ElapsedMs,
    DnsLookupResult Lookup);
