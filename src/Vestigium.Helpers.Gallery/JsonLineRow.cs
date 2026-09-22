using System.Text.Json;

namespace Vestigium.Helpers.Gallery;

public sealed class JsonLineRow
{
    public string Time { get; init; } = "";
    public string Level { get; init; } = "Information";
    public string Status { get; init; } = "None";
    public string AppId { get; init; } = "";
    public string Category { get; init; } = "";
    public string Message { get; init; } = "";
    public string Raw { get; init; } = "";

    public static JsonLineRow Parse(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var r = doc.RootElement;
            string Get(string name) =>
                r.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
                    ? p.GetString() ?? ""
                    : "";

            var dt = Get("DateTime");
            var time = dt.Length >= 19 ? dt[11..19] : dt;
            return new JsonLineRow
            {
                Time = time,
                Level = Get("LEVEL"),
                Status = Get("STATUS"),
                AppId = Get("APPID"),
                Category = Get("CATEGORY"),
                Message = Get("MESSAGE"),
                Raw = line
            };
        }
        catch (JsonException)
        {
            return new JsonLineRow { Message = line, Raw = line };
        }
    }
}
