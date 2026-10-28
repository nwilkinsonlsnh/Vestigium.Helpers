namespace Vestigium.Helpers.Network;

public enum PathMtuOutcome
{
    Passed,
    TooBig,
    Unknown
}

public sealed class PathMtuOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
    public int MinPayload { get; set; } = IcmpEchoOptions.MinBufferSize;
    public int MaxPayload { get; set; } = 1472;
    public int InterfaceIndex { get; set; }
    public string? SourceAddress { get; set; }
}

public sealed record PathMtuTry(
    int Payload,
    bool Passed,
    PathMtuOutcome Outcome,
    IcmpEchoStatus Status,
    long RoundtripTimeMs);

public sealed record PathMtuResult(
    string JobId,
    string Target,
    string? ResolvedAddress,
    NetworkJobStatus Status,
    int? LargestPayload,
    bool HitCeiling,
    IReadOnlyList<PathMtuTry> Tries);
