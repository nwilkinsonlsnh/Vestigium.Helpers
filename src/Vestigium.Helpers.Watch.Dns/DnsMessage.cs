namespace Vestigium.Helpers.Watch.Dns;

public static class DnsMessage
{
    public static bool TryRead(byte[]? payload, out string name, out string type, out bool response, out string status, out string answers)
    {
        name = "";
        type = "";
        response = false;
        status = "";
        answers = "";
        if (payload is null || payload.Length < 12)
            return false;

        response = (payload[2] & 0x80) != 0;
        var qd = (payload[4] << 8) | payload[5];
        var an = (payload[6] << 8) | payload[7];
        if (qd == 0)
            return false;

        var at = 12;
        if (!TryName(payload, ref at, out name) || at + 4 > payload.Length)
            return false;

        type = ((payload[at] << 8) | payload[at + 1]).ToString();
        at += 4;

        if (!response)
            return true;

        status = Rcode(payload[3] & 0x0F);
        var parts = new List<string>();
        for (var i = 0; i < an && at < payload.Length; i++)
        {
            if (!TryName(payload, ref at, out _))
                break;
            if (at + 10 > payload.Length)
                break;
            var rrType = (payload[at] << 8) | payload[at + 1];
            at += 8;
            var rdLength = (payload[at] << 8) | payload[at + 1];
            at += 2;
            if (at + rdLength > payload.Length)
                break;
            var data = Rdata(payload, at, rdLength, rrType);
            if (!string.IsNullOrEmpty(data))
                parts.Add(data);
            at += rdLength;
        }

        answers = string.Join("; ", parts);
        return true;
    }

    private static string Rcode(int code) => code switch
    {
        0 => "NOERROR",
        1 => "FORMERR",
        2 => "SERVFAIL",
        3 => "NXDOMAIN",
        4 => "NOTIMP",
        5 => "REFUSED",
        _ => code.ToString()
    };

    private static string Rdata(byte[] payload, int at, int length, int type)
    {
        if (type == 1 && length == 4)
            return payload[at] + "." + payload[at + 1] + "." + payload[at + 2] + "." + payload[at + 3];
        if (type == 28 && length == 16)
        {
            var parts = new string[8];
            for (var i = 0; i < 8; i++)
                parts[i] = ((payload[at + i * 2] << 8) | payload[at + i * 2 + 1]).ToString("x");
            return string.Join(":", parts);
        }
        if (type == 5)
        {
            var cursor = at;
            return TryName(payload, ref cursor, out var cname) ? cname : "";
        }
        return "";
    }

    private static bool TryName(byte[] payload, ref int at, out string name)
    {
        name = "";
        var labels = new List<string>();
        var jumped = false;
        var guard = 0;
        while (at < payload.Length && guard++ < 128)
        {
            var length = payload[at++];
            if (length == 0)
                break;
            if ((length & 0xC0) == 0xC0)
            {
                if (at >= payload.Length)
                    return false;
                var pointer = ((length & 0x3F) << 8) | payload[at++];
                if (!jumped)
                    at = at;
                jumped = true;
                at = pointer;
                continue;
            }
            if (at + length > payload.Length)
                return false;
            labels.Add(System.Text.Encoding.ASCII.GetString(payload, at, length));
            at += length;
        }

        if (labels.Count == 0)
            return false;
        name = string.Join(".", labels);
        return true;
    }
}
