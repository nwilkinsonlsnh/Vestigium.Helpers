using System.Collections.ObjectModel;

namespace Vestigium.Helpers.LogParser;

public sealed record LogReadResult
{
    public LogReadResult(
        LogFormat format,
        int entryCount,
        int pageCount,
        IReadOnlyList<LogHost> hosts,
        IReadOnlyList<string>? warnings = null)
    {
        if (entryCount < 0)
        {
            LogParserLog.Error(LogParserEvents.ResultRejected, Vestigium.Logging.VestigiumStatus.Failed, LogParserCatalog.Subcategories.Bag, "rejected entry count");
            throw new ArgumentOutOfRangeException(nameof(entryCount));
        }
        if (pageCount < 0)
        {
            LogParserLog.Error(LogParserEvents.ResultRejected, Vestigium.Logging.VestigiumStatus.Failed, LogParserCatalog.Subcategories.Bag, "rejected page count");
            throw new ArgumentOutOfRangeException(nameof(pageCount));
        }
        Format = format;
        EntryCount = entryCount;
        PageCount = pageCount;
        Hosts = Freeze(hosts);
        Warnings = FreezeWarnings(warnings);
    }

    public LogFormat Format { get; }

    public int EntryCount { get; }

    public int PageCount { get; }

    public IReadOnlyList<LogHost> Hosts { get; }

    public IReadOnlyList<string> Warnings { get; }

    private static IReadOnlyList<LogHost> Freeze(IReadOnlyList<LogHost> hosts)
    {
        ArgumentNullException.ThrowIfNull(hosts);
        if (hosts.Count == 0)
            return new ReadOnlyCollection<LogHost>([]);
        var copy = new LogHost[hosts.Count];
        for (var i = 0; i < hosts.Count; i++)
            copy[i] = hosts[i] ?? throw new ArgumentException("Host row is required.", nameof(hosts));
        return new ReadOnlyCollection<LogHost>(copy);
    }

    private static IReadOnlyList<string> FreezeWarnings(IReadOnlyList<string>? warnings)
    {
        if (warnings is null || warnings.Count == 0)
            return new ReadOnlyCollection<string>([]);
        var copy = new string[warnings.Count];
        for (var i = 0; i < warnings.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(warnings[i]))
                throw new ArgumentException("Warning text is required.", nameof(warnings));
            copy[i] = warnings[i];
        }
        return new ReadOnlyCollection<string>(copy);
    }
}
