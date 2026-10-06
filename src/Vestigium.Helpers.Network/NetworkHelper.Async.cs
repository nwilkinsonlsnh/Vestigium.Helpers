namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public static async Task<IReadOnlyList<NetworkRoute>> GetRoutesAsync(RouteFamily family = RouteFamily.All, CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        return await Task.Run(() => GetRoutes(family), cancellation).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<NetworkNeighbor>> GetNeighborsAsync(RouteFamily family = RouteFamily.All, CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        return await Task.Run(() => GetNeighbors(family), cancellation).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<NetworkConnection>> GetConnectionsAsync(NetworkConnectionQuery? query = null, CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        return await Task.Run(() => GetConnections(query), cancellation).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<NetworkLmHostEntry>> GetLmHostsAsync(CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        return await Task.Run(GetLmHosts, cancellation).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<NetworkNetBiosName>> GetNetBiosNamesAsync(CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        return await Task.Run(GetNetBiosNames, cancellation).ConfigureAwait(false);
    }

    public static async Task<NetworkNetBiosStats> GetNetBiosStatsAsync(CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        return await Task.Run(GetNetBiosStats, cancellation).ConfigureAwait(false);
    }
}
