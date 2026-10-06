using System.Collections.ObjectModel;

namespace Vestigium.Helpers.LogParser;

public sealed record LogHost
{
    public LogHost(string host, IEnumerable<int>? ports, int hitCount, LogHostSource sources, bool isAddress)
    {
        Host = Normalize(host);
        Ports = Freeze(ports);
        if (hitCount < 0)
            throw new ArgumentOutOfRangeException(nameof(hitCount));
        HitCount = hitCount;
        Sources = sources;
        IsAddress = isAddress;
    }

    public string Host { get; }

    public IReadOnlySet<int> Ports { get; }

    public int HitCount { get; }

    public LogHostSource Sources { get; }

    public bool IsAddress { get; }

    public static string Normalize(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host is required.", nameof(host));

        var trimmed = host.Trim().TrimEnd('.');
        if (trimmed.Length == 0)
            throw new ArgumentException("Host is required.", nameof(host));

        return trimmed.ToLowerInvariant();
    }

    private static IReadOnlySet<int> Freeze(IEnumerable<int>? ports)
    {
        if (ports is null)
            return Empty;

        var set = new SortedSet<int>();
        foreach (var port in ports)
        {
            if (port is < 1 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(ports), port, "Port must be 1..65535.");
            set.Add(port);
        }

        return set.Count == 0 ? Empty : new ReadOnlySet<int>(set);
    }

    private static readonly IReadOnlySet<int> Empty = new ReadOnlySet<int>(new SortedSet<int>());
}


public sealed record LogHost
{
    public LogHost(string host, IEnumerable<int>? ports, int hitCount, LogHostSource sources, bool isAddress)
    {
        Host = Normalize(host);
        Ports = Freeze(ports);
        if (hitCount < 0)
            throw new ArgumentOutOfRangeException(nameof(hitCount));
        HitCount = hitCount;
        Sources = sources;
        IsAddress = isAddress;
    }

    public string Host { get; }

    public IReadOnlySet<int> Ports { get; }

    public int HitCount { get; }

    public LogHostSource Sources { get; }

    public bool IsAddress { get; }

    public static string Normalize(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host is required.", nameof(host));

        var trimmed = host.Trim().TrimEnd('.');
        if (trimmed.Length == 0)
            throw new ArgumentException("Host is required.", nameof(host));

        return trimmed.ToLowerInvariant();
    }

    private static IReadOnlySet<int> Freeze(IEnumerable<int>? ports)
    {
        if (ports is null)
            return Empty;

        var set = new SortedSet<int>();
        foreach (var port in ports)
        {
            if (port is < 1 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(ports), port, "Port must be 1..65535.");
            set.Add(port);
        }

        return set.Count == 0 ? Empty : set;
    }

    private static readonly IReadOnlySet<int> Empty = new SortedSet<int>();
}
