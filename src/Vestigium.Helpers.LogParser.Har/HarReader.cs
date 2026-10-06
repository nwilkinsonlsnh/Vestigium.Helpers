using System.Net;
using System.Text.Json;
using Vestigium.Helpers.LogParser;

namespace Vestigium.Helpers.LogParser.Har;

public static class HarReader
{
    public const long MaxBytes = 64L * 1024 * 1024;

    public static LogReadResult ReadFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        var info = new FileInfo(path);
        if (!info.Exists)
            throw new FileNotFoundException("HAR file was not found.", path);
        if (info.Length > MaxBytes)
            throw new InvalidDataException("HAR file is over 64 MB.");

        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    public static LogReadResult Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (stream.CanSeek && stream.Length > MaxBytes)
            throw new InvalidDataException("HAR file is over 64 MB.");

        using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        if (!document.RootElement.TryGetProperty("log", out var log) || log.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("HAR is missing log.");
        if (!log.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("HAR is missing log.entries.");

        var rows = new Dictionary<string, Bucket>(StringComparer.Ordinal);
        var order = new List<Bucket>();

        var pages = 0;
        if (log.TryGetProperty("pages", out var pageArray) && pageArray.ValueKind == JsonValueKind.Array)
        {
            pages = pageArray.GetArrayLength();
            foreach (var page in pageArray.EnumerateArray())
            {
                if (page.TryGetProperty("title", out var title))
                    Add(rows, order, title.GetString(), LogHostSource.Page, countHit: false);
            }
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.TryGetProperty("request", out var request) && request.TryGetProperty("url", out var url))
                Add(rows, order, url.GetString(), LogHostSource.Request, countHit: true);

            if (!entry.TryGetProperty("response", out var response))
                continue;

            if (response.TryGetProperty("redirectURL", out var redirect))
                Add(rows, order, redirect.GetString(), LogHostSource.Redirect, countHit: false);

            if (response.TryGetProperty("headers", out var headers) && headers.ValueKind == JsonValueKind.Array)
            {
                foreach (var header in headers.EnumerateArray())
                {
                    if (!header.TryGetProperty("name", out var name) || !header.TryGetProperty("value", out var value))
                        continue;
                    if (!string.Equals(name.GetString(), "Location", StringComparison.OrdinalIgnoreCase))
                        continue;
                    Add(rows, order, value.GetString(), LogHostSource.Location, countHit: false);
                }
            }
        }

        var warnings = new List<string>();
        if (!log.TryGetProperty("version", out var version) || version.GetString() != "1.2")
            warnings.Add("log.version is not 1.2.");

        var hosts = new LogHost[order.Count];
        for (var i = 0; i < order.Count; i++)
        {
            var row = order[i];
            hosts[i] = new LogHost(row.Host, row.Ports, row.Hits, row.Sources, row.IsAddress);
        }

        return new LogReadResult(LogFormat.Har, entries.GetArrayLength(), pages, hosts, warnings);
    }

    private static void Add(
        Dictionary<string, Bucket> rows,
        List<Bucket> order,
        string? raw,
        LogHostSource source,
        bool countHit)
    {
        if (!TryHost(raw, out var host, out var port, out var address))
            return;
        if (!rows.TryGetValue(host, out var row))
        {
            row = new Bucket(host, address);
            rows.Add(host, row);
            order.Add(row);
        }
        row.Sources |= source;
        if (countHit)
            row.Hits++;
        if (port is int seen)
            row.Ports.Add(seen);
    }

    private static bool TryHost(string? raw, out string host, out int? port, out bool isAddress)
    {
        host = "";
        port = null;
        isAddress = false;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme is not "http" and not "https")
            return false;
        if (string.IsNullOrWhiteSpace(uri.IdnHost))
            return false;

        host = LogHost.Normalize(uri.IdnHost);
        isAddress = IPAddress.TryParse(uri.IdnHost, out _);
        if (!uri.IsDefaultPort)
            port = uri.Port;
        return true;
    }

    private sealed class Bucket(string host, bool isAddress)
    {
        public string Host { get; } = host;
        public bool IsAddress { get; } = isAddress;
        public int Hits { get; set; }
        public LogHostSource Sources { get; set; }
        public SortedSet<int> Ports { get; } = [];
    }
}
