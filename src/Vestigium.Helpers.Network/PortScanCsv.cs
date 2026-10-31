using System.Text;

namespace Vestigium.Helpers.Network;

public static class PortScanCsv
{
    public static string Write(PortScanResult result)
    {
        var text = new StringBuilder();
        text.AppendLine("host,port,transport,service,mss,banner");
        foreach (var row in result.Hosts)
        {
            foreach (var port in row.OpenTcp)
                Append(text, row.Host, port);
            foreach (var port in row.OpenUdp)
                Append(text, row.Host, port);
        }

        return text.ToString();
    }

    private static void Append(StringBuilder text, string host, PortScanOpenPort port)
    {
        text.Append(host).Append(',').Append(port.Port).Append(',').Append(port.Transport.ToString().ToLowerInvariant()).Append(',');
        text.Append(port.Service ?? "").Append(',');
        text.Append(port.Mss?.ToString() ?? "").Append(',');
        text.Append('"').Append((port.Banner ?? "").Replace("\"", "\"\"")).Append('"');
        text.AppendLine();
    }
}
