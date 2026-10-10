namespace Vestigium.Helpers.Watch.Dns;

public sealed class WatchRollup
{
    private readonly Dictionary<string, WatchRow> _rows = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    public int Keys
    {
        get
        {
            lock (_gate)
                return _rows.Count;
        }
    }

    public WatchRow? Add(string? name, string? type, WatchSource source, int pid, string? status = null, string? answers = null, bool response = false)
    {
        if (source == WatchSource.Both)
            throw new ArgumentOutOfRangeException(nameof(source), "A row comes from Event or Port.");
        if (!QueryName.TryNormalize(name, out var keyName, out _))
            return null;

        var keyType = DnsQueryTypes.Name(type);
        var key = keyName + " " + keyType;
        lock (_gate)
        {
            _rows.TryGetValue(key, out var current);
            var resolver = current?.ResolverCount ?? 0;
            var packet = current?.PacketCount ?? 0;
            if (source == WatchSource.Event)
                resolver++;
            else if (response)
            {
                packet++;
                if (resolver < packet)
                    resolver = packet;
            }
            else
                resolver++;

            var keptPid = current is null
                ? pid
                : current.Pid == pid ? pid : 0;

            var keptStatus = Keep(current?.Status, status);
            var keptAnswers = Keep(current?.Answers, answers);

            var row = new WatchRow(DateTimeOffset.UtcNow, "", keptPid, keyName, keyType, keptStatus, keptAnswers, "rollup")
            {
                ResolverCount = resolver,
                PacketCount = packet,
                Total = resolver + packet
            };
            _rows[key] = row;
            return row;
        }
    }

    private static string Keep(string? current, string? incoming)
    {
        if (!string.IsNullOrWhiteSpace(incoming))
            return incoming.Trim();
        return current ?? "";
    }
}
