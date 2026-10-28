namespace Vestigium.Helpers.Network;

public sealed record NeighborProbeResult(
    string Address,
    string? MacAddress,
    bool Found);
