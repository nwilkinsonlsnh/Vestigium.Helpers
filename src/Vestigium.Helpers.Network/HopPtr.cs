using System.Net;

namespace Vestigium.Helpers.Network;

internal static class HopPtr
{
    public static async Task<string?> LookupAsync(string? address, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(address) || !IPAddress.TryParse(address, out _))
            return null;

        try
        {
            var result = await DnsClient.LookupAsync(
                address,
                new DnsLookupOptions { Type = DnsRecordType.Ptr, Timeout = TimeSpan.FromSeconds(2) },
                token).ConfigureAwait(false);
            if (result.Rcode != DnsRcode.NoError)
                return null;
            var name = result.Answers.FirstOrDefault(a => a.Type == DnsRecordType.Ptr)?.Data;
            return string.IsNullOrWhiteSpace(name) ? null : name.Trim().TrimEnd('.');
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static async Task<IReadOnlyList<IcmpTraceHop>> FillTraceAsync(
        IReadOnlyList<IcmpTraceHop> hops,
        CancellationToken token)
    {
        var rows = new IcmpTraceHop[hops.Count];
        for (var i = 0; i < hops.Count; i++)
        {
            var hop = hops[i];
            if (token.IsCancellationRequested)
            {
                rows[i] = hop;
                continue;
            }

            var name = hop.Name ?? await LookupAsync(hop.Address, token).ConfigureAwait(false);
            rows[i] = hop with { Name = name };
        }

        return rows;
    }
}
