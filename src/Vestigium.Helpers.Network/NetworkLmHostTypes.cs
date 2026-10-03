namespace Vestigium.Helpers.Network;

public sealed record NetworkLmHostEntry(
    string Address,
    string Name,
    bool Preload,
    bool MultiHome,
    string? Domain,
    string? IncludePath,
    string? Raw);
