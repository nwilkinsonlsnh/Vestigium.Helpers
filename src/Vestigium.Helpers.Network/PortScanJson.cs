using System.Text.Json;

namespace Vestigium.Helpers.Network;

public static class PortScanJson
{
    public static string Write(PortScanResult result)
    {
        var payload = new PortScanJsonDocument(
            result.JobId,
            result.Status.ToString(),
            result.HostCount,
            result.PortCount,
            result.OpenCount,
            PortScanCsv.Rows(result));
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private sealed record PortScanJsonDocument(
        string JobId,
        string Status,
        int HostCount,
        int PortCount,
        int OpenCount,
        IReadOnlyList<PortScanOpenRow> Rows);
}
