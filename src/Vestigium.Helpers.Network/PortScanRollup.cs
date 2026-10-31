namespace Vestigium.Helpers.Network;

public static class PortScanRollup
{
    public static IReadOnlyList<PortScanHit> Name(IEnumerable<PortScanHit> hits)
        => hits.Select(NameOne).ToArray();

    public static IReadOnlyList<PortScanHostRow> Hosts(
        IEnumerable<PortScanHit> hits,
        bool infer,
        IReadOnlyDictionary<string, string?>? macs = null)
    {
        return hits
            .GroupBy(static hit => hit.Host, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => Row(group.Key, group.ToArray(), infer, macs))
            .ToArray();
    }

    private static PortScanHit NameOne(PortScanHit hit)
    {
        if (!string.IsNullOrWhiteSpace(hit.Service))
            return hit;
        var transport = hit.Transport == PortScanTransport.Udp ? "UDP" : "TCP";
        var service = NetworkPorts.Try(transport, hit.Port, out var guess) ? guess.Name : null;
        return hit with { Service = service };
    }

    private static PortScanHostRow Row(
        string host,
        PortScanHit[] hits,
        bool infer,
        IReadOnlyDictionary<string, string?>? macs)
    {
        var open = hits.Where(static hit => hit.Status == PortScanStatus.Open).Select(NameOne).ToArray();
        var tcp = open.Where(static hit => hit.Transport == PortScanTransport.Tcp).Select(Open).ToArray();
        var udp = open.Where(static hit => hit.Transport == PortScanTransport.Udp).Select(Open).ToArray();
        var mac = macs is not null && macs.TryGetValue(host, out var found) ? found : null;
        return new PortScanHostRow(host, tcp, udp, infer ? Score(open) : null, mac);
    }

    private static PortScanOpenPort Open(PortScanHit hit)
        => new(hit.Port, hit.Transport, hit.Service, hit.Mss, hit.Banner, hit.ElapsedMs);

    private static InferenceResult Score(IReadOnlyList<PortScanHit> open)
    {
        var evidence = new List<InferenceEvidence>();
        foreach (var hit in open)
        {
            evidence.Add(new InferenceEvidence(InferenceEvidenceKind.PortOpen, hit.Service ?? hit.Port.ToString(), hit.Port));
            if (!string.IsNullOrWhiteSpace(hit.Banner))
                evidence.Add(new InferenceEvidence(InferenceEvidenceKind.Banner, hit.Banner));
        }

        return DeviceInference.Score(evidence);
    }
}
