namespace Vestigium.Helpers.Network;

public enum DnsRecordType : ushort
{
    A = 1,
    Ns = 2,
    Cname = 5,
    Soa = 6,
    Ptr = 12,
    Mx = 15,
    Txt = 16,
    Aaaa = 28,
    Srv = 33,
    Any = 255
}

public enum DnsRcode
{
    NoError = 0,
    FormErr = 1,
    ServFail = 2,
    NxDomain = 3,
    NotImp = 4,
    Refused = 5,
    Timeout = 16,
    Failed = 17
}

public sealed class DnsLookupOptions
{
    public DnsRecordType Type { get; set; } = DnsRecordType.A;
    public string? Server { get; set; }
    public int Port { get; set; } = 53;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);
    public bool RecursionDesired { get; set; } = true;
}

public sealed record DnsRecord(
    DnsRecordType Type,
    string Name,
    int Ttl,
    string Data);

public sealed record DnsLookupResult(
    string Question,
    DnsRecordType Type,
    string? Server,
    DnsRcode Rcode,
    bool Truncated,
    bool UsedTcp,
    TimeSpan Elapsed,
    IReadOnlyList<DnsRecord> Answers);
