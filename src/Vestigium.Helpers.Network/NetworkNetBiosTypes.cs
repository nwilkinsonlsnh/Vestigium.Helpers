namespace Vestigium.Helpers.Network;

public sealed record NetworkNetBiosName(
    string Table,
    string Name,
    string Suffix,
    string Type,
    string Status,
    string? Address,
    int? LifeSeconds);
