namespace Vestigium.Helpers.Network;

public sealed record PortSet(IReadOnlyList<int> Ports)
{
    public const int MaxPorts = 1024;

    public static PortSet FromList(IEnumerable<int>? ports)
        => new(Expand(ports ?? []));

    public static PortSet FromRange(int begin, int end)
    {
        if (begin is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(begin), "Port must be 1–65535.");
        if (end is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(end), "Port must be 1–65535.");
        if (begin > end)
            throw new ArgumentOutOfRangeException(nameof(end), "End must be greater than or equal to begin.");

        var count = end - begin + 1;
        if (count > MaxPorts)
            throw new ArgumentOutOfRangeException(nameof(end), $"Range expands to {count} ports. Cap is {MaxPorts}.");

        var ports = new int[count];
        for (var i = 0; i < count; i++)
            ports[i] = begin + i;
        return new PortSet(ports);
    }

    public static PortSet WellKnown(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Well-known set name is required.", nameof(name));

        var key = name.Trim();
        if (key.Equals("common", StringComparison.OrdinalIgnoreCase))
            return FromList(Common);
        if (key.Equals("catalog", StringComparison.OrdinalIgnoreCase))
            return FromList(NetworkPorts.All.Select(static row => row.Port));

        throw new ArgumentException($"Unknown well-known set '{name}'.", nameof(name));
    }

    private static int[] Expand(IEnumerable<int> ports)
    {
        var set = new SortedSet<int>();
        foreach (var port in ports)
        {
            if (port is < 1 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(ports), "Port must be 1–65535.");
            set.Add(port);
            if (set.Count > MaxPorts)
                throw new ArgumentOutOfRangeException(nameof(ports), $"Port set expands past {MaxPorts}.");
        }

        return set.ToArray();
    }

    private static readonly int[] Common =
    [
        22, 53, 80, 88, 135, 139, 161, 389, 443, 445,
        515, 631, 1433, 1521, 3306, 3389, 5432, 5900, 5985, 5986,
        8080, 8443, 9100
    ];
}
