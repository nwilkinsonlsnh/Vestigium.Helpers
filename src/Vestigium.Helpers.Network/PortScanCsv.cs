using System.Text;

namespace Vestigium.Helpers.Network;

public sealed record PortScanOpenRow(
    string Host,
    int Port,
    PortScanTransport Transport,
    string? Service,
    int? Mss,
    string? Banner,
    long ElapsedMs);

public static class PortScanCsv
{
    public static IReadOnlyList<PortScanOpenRow> Rows(PortScanResult result)
    {
        var rows = new List<PortScanOpenRow>();
        foreach (var host in result.Hosts)
        {
            foreach (var port in host.OpenTcp)
                rows.Add(new PortScanOpenRow(host.Host, port.Port, port.Transport, port.Service, port.Mss, port.Banner, port.ElapsedMs));
            foreach (var port in host.OpenUdp)
                rows.Add(new PortScanOpenRow(host.Host, port.Port, port.Transport, port.Service, port.Mss, port.Banner, port.ElapsedMs));
        }

        return rows;
    }

    public static string Write(PortScanResult result)
    {
        var text = new StringBuilder();
        text.AppendLine("host,port,transport,service,mss,banner");
        foreach (var row in Rows(result))
            Append(text, row);
        return text.ToString();
    }

    private static void Append(StringBuilder text, PortScanOpenRow row)
    {
        text.Append(row.Host).Append(',').Append(row.Port).Append(',').Append(row.Transport.ToString().ToLowerInvariant()).Append(',');
        text.Append(row.Service ?? "").Append(',');
        text.Append(row.Mss?.ToString() ?? "").Append(',');
        text.Append('"').Append((row.Banner ?? "").Replace("\"", "\"\"")).Append('"');
        text.AppendLine();
    }
}
