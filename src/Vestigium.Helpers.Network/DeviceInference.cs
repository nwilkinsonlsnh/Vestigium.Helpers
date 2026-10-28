namespace Vestigium.Helpers.Network;

public static class DeviceInference
{
    private static readonly int[] WindowsPorts = [135, 139, 445, 3389, 5985, 5986];
    private static readonly int[] NetworkPorts = [161, 162, 23];
    private static readonly int[] UnixPorts = [22];
    private static readonly int[] PrinterPorts = [515, 631, 9100];
    private static readonly int[] MobilePorts = [5555];

    public static InferenceResult Score(IEnumerable<InferenceEvidence>? evidence)
    {
        var rows = (evidence ?? []).Where(static row => row is not null && !string.IsNullOrWhiteSpace(row.Detail)).ToArray();
        if (rows.Length == 0)
            return new InferenceResult(InferredDeviceClass.Unknown, InferredVendor.Unknown, InferenceConfidence.None, []);

        var used = new List<InferenceEvidence>(rows.Length);
        var windows = 0;
        var unix = 0;
        var network = 0;
        var printer = 0;
        var mobile = 0;
        var vendorHits = new Dictionary<InferredVendor, int>();
        var independent = 0;

        foreach (var row in rows)
        {
            var contributed = false;
            switch (row.Kind)
            {
                case InferenceEvidenceKind.PortOpen:
                    contributed = ScorePort(row, ref windows, ref unix, ref network, ref printer, ref mobile);
                    break;
                case InferenceEvidenceKind.OuiMatch:
                case InferenceEvidenceKind.Banner:
                case InferenceEvidenceKind.SnmpSysDescr:
                    contributed = ScoreText(row, ref network, ref printer, ref mobile, vendorHits);
                    break;
            }

            if (contributed)
            {
                used.Add(row);
                independent++;
            }
        }

        var device = DecideClass(windows, unix, network, printer, mobile);
        var vendor = DecideVendor(vendorHits);
        var confidence = DecideConfidence(independent, device, vendor);
        return new InferenceResult(device, vendor, confidence, used);
    }

    private static bool ScorePort(InferenceEvidence row, ref int windows, ref int unix, ref int network, ref int printer, ref int mobile)
    {
        var port = row.Port ?? TryPort(row.Detail);
        if (port is null)
            return false;

        if (WindowsPorts.Contains(port.Value))
        {
            windows++;
            return true;
        }

        if (NetworkPorts.Contains(port.Value))
        {
            network++;
            return true;
        }

        if (PrinterPorts.Contains(port.Value))
        {
            printer++;
            return true;
        }

        if (MobilePorts.Contains(port.Value))
        {
            mobile++;
            return true;
        }

        if (UnixPorts.Contains(port.Value))
        {
            unix++;
            return true;
        }

        return false;
    }

    private static bool ScoreText(InferenceEvidence row, ref int network, ref int printer, ref int mobile, Dictionary<InferredVendor, int> vendorHits)
    {
        var text = row.Detail;
        var vendor = MatchVendor(text);
        var contributed = false;
        if (vendor != InferredVendor.Unknown)
        {
            vendorHits[vendor] = vendorHits.GetValueOrDefault(vendor) + 1;
            contributed = true;
        }

        if (ContainsAny(text, "cisco", "ios", "nx-os", "junos", "arista", "pan-os", "fortios", "router", "switch"))
        {
            network++;
            contributed = true;
        }

        if (ContainsAny(text, "printer", "jetdirect", "cups"))
        {
            printer++;
            contributed = true;
        }

        if (ContainsAny(text, "android", "adb"))
        {
            mobile++;
            contributed = true;
        }

        return contributed;
    }

    private static InferredDeviceClass DecideClass(int windows, int unix, int network, int printer, int mobile)
    {
        var ranked = new (InferredDeviceClass Class, int Score)[]
        {
            (InferredDeviceClass.WindowsHost, windows),
            (InferredDeviceClass.UnixLikeHost, unix),
            (InferredDeviceClass.NetworkDevice, network),
            (InferredDeviceClass.PrinterOrAppliance, printer),
            (InferredDeviceClass.MobileOrEmbedded, mobile)
        };
        var best = ranked.OrderByDescending(row => row.Score).First();
        return best.Score == 0 ? InferredDeviceClass.Unknown : best.Class;
    }

    private static InferredVendor DecideVendor(Dictionary<InferredVendor, int> hits)
    {
        if (hits.Count == 0)
            return InferredVendor.Unknown;
        return hits.OrderByDescending(pair => pair.Value).First().Key;
    }

    private static InferenceConfidence DecideConfidence(int independent, InferredDeviceClass device, InferredVendor vendor)
    {
        if (independent == 0 || device == InferredDeviceClass.Unknown && vendor == InferredVendor.Unknown)
            return InferenceConfidence.None;
        if (independent >= 3)
            return InferenceConfidence.High;
        if (independent == 2)
            return InferenceConfidence.Medium;
        return InferenceConfidence.Low;
    }

    private static InferredVendor MatchVendor(string text)
    {
        if (ContainsAny(text, "cisco"))
            return InferredVendor.Cisco;
        if (ContainsAny(text, "juniper", "junos"))
            return InferredVendor.Juniper;
        if (ContainsAny(text, "arista"))
            return InferredVendor.Arista;
        if (ContainsAny(text, "palo alto", "paloalto", "pan-os"))
            return InferredVendor.PaloAlto;
        if (ContainsAny(text, "fortinet", "fortigate", "fortios"))
            return InferredVendor.Fortinet;
        return InferredVendor.Unknown;
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (text.Contains(needle, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static int? TryPort(string detail)
    {
        var token = detail.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return int.TryParse(token, out var port) && port is >= 1 and <= 65535 ? port : null;
    }
}
