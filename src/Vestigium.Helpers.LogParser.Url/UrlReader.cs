using System.Net;
using System.Text.RegularExpressions;
using Vestigium.Helpers.LogParser;

namespace Vestigium.Helpers.LogParser.Url;

public static partial class UrlReader
{
    public const long MaxBytes = 64L * 1024 * 1024;

    private static readonly HashSet<string> FileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "txt", "csv", "tsv", "xlsx", "xls", "json", "har", "log", "xml",
        "pdf", "png", "jpg", "jpeg", "gif", "dll", "exe", "config", "md"
    };

    public static LogReadResult ReadFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        var info = new FileInfo(path);
        if (!info.Exists)
            throw new FileNotFoundException("Text file was not found.", path);
        if (info.Length > MaxBytes)
        {
            UrlLog.Error(UrlEvents.Oversize, "text over 64 MB");
            throw new InvalidDataException("Text file is over 64 MB.");
        }

        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    public static LogReadResult Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (stream.CanSeek && stream.Length > MaxBytes)
        {
            UrlLog.Error(UrlEvents.Oversize, "text over 64 MB");
            throw new InvalidDataException("Text file is over 64 MB.");
        }

        UrlLog.Information(UrlEvents.ScanStart, "scan start");

        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        var covered = new bool[text.Length];
        var rows = new Dictionary<string, Bucket>(StringComparer.Ordinal);
        var order = new List<Bucket>();

        foreach (Match match in SchemePattern().Matches(text))
        {
            Cover(covered, match.Index, match.Length);
            if (!Uri.TryCreate(match.Value, UriKind.Absolute, out var uri))
                continue;
            if (uri.Scheme is not "http" and not "https")
                continue;
            if (string.IsNullOrWhiteSpace(uri.IdnHost))
                continue;
            Add(rows, order, uri.IdnHost, uri.IsDefaultPort ? null : uri.Port, IPAddress.TryParse(uri.IdnHost, out _));
        }

        foreach (Match match in MailPattern().Matches(text))
        {
            if (Overlaps(covered, match.Index, match.Length))
                continue;
            Cover(covered, match.Index, match.Length);
            Add(rows, order, match.Groups["host"].Value, null, false);
        }

        foreach (Match match in AddressPattern().Matches(text))
        {
            if (Overlaps(covered, match.Index, match.Length))
                continue;
            if (!IPAddress.TryParse(match.Value, out var address) || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                continue;
            Cover(covered, match.Index, match.Length);
            Add(rows, order, match.Value, null, true);
        }

        foreach (Match match in BarePattern().Matches(text))
        {
            if (Overlaps(covered, match.Index, match.Length))
                continue;
            if (!AcceptBare(match.Value))
                continue;
            Cover(covered, match.Index, match.Length);
            Add(rows, order, match.Value, null, false);
        }

        foreach (Match match in LocalhostPattern().Matches(text))
        {
            if (Overlaps(covered, match.Index, match.Length))
                continue;
            Add(rows, order, "localhost", null, false);
        }

        var hosts = new LogHost[order.Count];
        for (var i = 0; i < order.Count; i++)
        {
            var row = order[i];
            hosts[i] = new LogHost(row.Host, row.Ports, row.Hits, LogHostSource.Url, row.IsAddress);
        }

        UrlLog.Information(UrlEvents.ScanComplete, "scan complete");
        return new LogReadResult(LogFormat.Url, hosts.Length, 0, hosts);
    }

    private static void Add(
        Dictionary<string, Bucket> rows,
        List<Bucket> order,
        string raw,
        int? port,
        bool isAddress)
    {
        var host = LogHost.Normalize(raw);
        if (!rows.TryGetValue(host, out var row))
        {
            row = new Bucket(host, isAddress);
            rows.Add(host, row);
            order.Add(row);
        }
        row.Hits++;
        if (port is int seen)
            row.Ports.Add(seen);
    }

    private static bool AcceptBare(string token)
    {
        var labels = token.Split('.');
        if (labels.Length < 2)
            return false;
        if (FileExtensions.Contains(labels[^1]))
            return false;
        foreach (var label in labels)
        {
            if (label.Length is < 2 or > 63)
                return false;
            if (label[0] == '-' || label[^1] == '-')
                return false;
            if (label.All(char.IsDigit))
                return false;
        }
        return labels[^1].All(char.IsLetter);
    }

    private static void Cover(bool[] covered, int index, int length)
    {
        var end = Math.Min(covered.Length, index + length);
        for (var i = Math.Max(0, index); i < end; i++)
            covered[i] = true;
    }

    private static bool Overlaps(bool[] covered, int index, int length)
    {
        var end = Math.Min(covered.Length, index + length);
        for (var i = Math.Max(0, index); i < end; i++)
        {
            if (covered[i])
                return true;
        }
        return false;
    }

    [GeneratedRegex(@"https?://[^\s<>""']+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SchemePattern();

    [GeneratedRegex(@"(?i)(?:mailto:)?[a-z0-9._%+-]+@(?<host>[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?)+)")]
    private static partial Regex MailPattern();

    [GeneratedRegex(@"(?<!\d)(?:\d{1,3}\.){3}\d{1,3}(?!\d)")]
    private static partial Regex AddressPattern();

    [GeneratedRegex(@"(?i)(?<![@\w])[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?)+(?![\w])")]
    private static partial Regex BarePattern();

    [GeneratedRegex(@"(?i)(?<![@\w.])localhost(?![\w.])")]
    private static partial Regex LocalhostPattern();

    private sealed class Bucket(string host, bool isAddress)
    {
        public string Host { get; } = host;
        public bool IsAddress { get; } = isAddress;
        public int Hits { get; set; }
        public SortedSet<int> Ports { get; } = [];
    }
}
