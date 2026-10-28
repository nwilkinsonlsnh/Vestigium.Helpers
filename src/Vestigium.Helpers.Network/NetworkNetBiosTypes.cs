namespace Vestigium.Helpers.Network;

public sealed record NetworkNetBiosName(
    string Table,
    string Name,
    string Suffix,
    string SuffixName,
    string Type,
    string Status,
    string? Address,
    int? LifeSeconds,
    string? Adapter,
    string? NodeAddress,
    bool IsCache);

public sealed record NetworkNetBiosStats(
    int ResolvedByBroadcast,
    int ResolvedByNameServer,
    int RegisteredByBroadcast,
    int RegisteredByNameServer,
    string? NodeType);
