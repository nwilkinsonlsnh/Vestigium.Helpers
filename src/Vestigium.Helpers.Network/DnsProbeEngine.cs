using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class DnsProbeEngine
{
    public static NetworkJob<DnsProbeResult> Create(string name, DnsLookupOptions? options)
    {
        var host = HelperGuard.NotBlank(name, nameof(name)).Trim();
        var o = options ?? new DnsLookupOptions();
        EgressBind.Validate(o.InterfaceIndex, o.SourceAddress);
        var jobId = "dns-" + HelperLog.NewId();
        return new NetworkJob<DnsProbeResult>(jobId, "probeDns", (token, progress) => RunAsync(jobId, host, o, token, progress));
    }

    internal static DnsProbeStatus Map(DnsRcode rcode) => rcode switch
    {
        DnsRcode.Timeout => DnsProbeStatus.TimedOut,
        DnsRcode.Refused => DnsProbeStatus.Refused,
        _ => DnsProbeStatus.Answered
    };

    private static async Task<DnsProbeResult> RunAsync(
        string jobId,
        string name,
        DnsLookupOptions options,
        CancellationToken token,
        IProgress<NetworkProgress>? progress)
    {
        NetworkLog.Pending(HelperLog.Subcategories.Icmp, $"probeDns job={jobId} name={name} server={options.Server ?? "os"}");
        var lookup = await DnsClient.LookupAsync(name, options, token).ConfigureAwait(false);
        var status = Map(lookup.Rcode);
        var result = new DnsProbeResult(
            jobId,
            name,
            lookup.Server ?? options.Server,
            status,
            lookup.Rcode,
            (long)lookup.Elapsed.TotalMilliseconds,
            lookup);
        progress?.Report(new NetworkProgress
        {
            JobId = jobId,
            Phase = "Dns",
            LastStatus = status.ToString(),
            LastRoundtripMs = result.ElapsedMs,
            Received = lookup.Answers.Count
        });
        var line = $"{status} probeDns job={jobId} name={name} rcode={lookup.Rcode} ms={result.ElapsedMs}";
        if (status == DnsProbeStatus.Answered)
            NetworkLog.Success(HelperLog.Subcategories.Icmp, line);
        else
            NetworkLog.Failed(HelperLog.Subcategories.Icmp, line);
        return result;
    }
}
