using System.Text;

namespace Vestigium.Helpers.Network.RawSocket;

public sealed record SynScanOpenRow(
    string Host,
    int Port,
    long ElapsedMs,
    string? Detail);

public static class SynScanCsv
{
    public static IReadOnlyList<SynScanOpenRow> Rows(SynScanResult result)
    {
        var rows = new List<SynScanOpenRow>();
        foreach (var hit in result.Hits)
        {
            if (hit.Status != SynPortStatus.Open)
                continue;
            rows.Add(new SynScanOpenRow(hit.Host, hit.Port, hit.ElapsedMs, hit.Detail));
        }

        return rows;
    }

    public static string Write(SynScanResult result)
    {
        var text = new StringBuilder();
        text.AppendLine("host,port,elapsed,detail");
        foreach (var row in Rows(result))
        {
            text.Append(row.Host).Append(',').Append(row.Port).Append(',').Append(row.ElapsedMs).Append(',');
            text.Append('"').Append((row.Detail ?? "").Replace("\"", "\"\"")).Append('"');
            text.AppendLine();
        }

        return text.ToString();
    }
}
