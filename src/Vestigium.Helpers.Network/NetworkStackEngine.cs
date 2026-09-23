using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class NetworkStackEngine
{
    public static IReadOnlyList<NetworkConnection> GetConnections(NetworkConnectionQuery? query)
    {
        query ??= new NetworkConnectionQuery();
        var rows = new List<NetworkConnection>();
        try
        {
            var props = IPGlobalProperties.GetIPGlobalProperties();
            if (query.Protocol is null or TransportProtocol.Tcp)
            {
                rows.AddRange(props.GetActiveTcpConnections().Select(c => MapTcp(c)));
                rows.AddRange(props.GetActiveTcpListeners().Select(ep => MapListener(TransportProtocol.Tcp, ep, "Listen")));
            }

            if (query.Protocol is null or TransportProtocol.Udp)
            {
                rows.AddRange(props.GetActiveUdpListeners().Select(ep => MapListener(TransportProtocol.Udp, ep, "Listen")));
            }
        }
        catch (NetworkInformationException)
        {
        }

        EnrichPids(rows);

        IEnumerable<NetworkConnection> filtered = rows;
        if (query.ListeningOnly)
            filtered = filtered.Where(r => string.Equals(r.State, "Listen", StringComparison.OrdinalIgnoreCase));
        if (query.EstablishedOnly)
            filtered = filtered.Where(r => string.Equals(r.State, "Established", StringComparison.OrdinalIgnoreCase));
        return
        [
            .. filtered
                .DistinctBy(r =>
                    $"{r.Protocol}|{r.LocalAddress}|{r.LocalPort}|{r.RemoteAddress}|{r.RemotePort}|{r.State}")
        ];
    }

    public static NetworkStackStatistics GetStatistics()
    {
        TcpStatisticsSnapshot? t4 = null, t6 = null;
        UdpStatisticsSnapshot? u4 = null, u6 = null;
        try
        {
            var props = IPGlobalProperties.GetIPGlobalProperties();
            t4 = MapTcp(props.GetTcpIPv4Statistics());
            u4 = MapUdp(props.GetUdpIPv4Statistics());
            try { t6 = MapTcp(props.GetTcpIPv6Statistics()); } catch (NetworkInformationException) { } catch (PlatformNotSupportedException) { }
            try { u6 = MapUdp(props.GetUdpIPv6Statistics()); } catch (NetworkInformationException) { } catch (PlatformNotSupportedException) { }
        }
        catch (NetworkInformationException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }

        return new NetworkStackStatistics(t4, t6, u4, u6);
    }

    public static IReadOnlyList<NetworkRoute> GetRoutes(RouteFamily family)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? NetworkWindowsTables.GetRoutes(family) : NetworkLinuxTables.GetRoutes(family);
    }

    public static IReadOnlyList<NetworkNeighbor> GetNeighbors()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? NetworkWindowsTables.GetNeighbors() : NetworkLinuxTables.GetNeighbors();
    }

    private static NetworkConnection MapTcp(TcpConnectionInformation c)
    {
        var local = c.LocalEndPoint;
        var remote = c.RemoteEndPoint;
        return new NetworkConnection(
            TransportProtocol.Tcp,
            local.AddressFamily,
            local.Address.ToString(),
            local.Port,
            remote.Port == 0 ? null : remote.Address.ToString(),
            remote.Port == 0 ? null : remote.Port,
            c.State.ToString(),
            null,
            null);
    }

    private static NetworkConnection MapListener(TransportProtocol protocol, IPEndPoint ep, string state)
        => new(
            protocol,
            ep.AddressFamily,
            ep.Address.ToString(),
            ep.Port,
            null,
            null,
            state,
            null,
            null);

    private static TcpStatisticsSnapshot MapTcp(TcpStatistics s)
        => new(
            s.ConnectionsAccepted,
            s.ConnectionsInitiated,
            s.CumulativeConnections,
            s.CurrentConnections,
            s.FailedConnectionAttempts,
            s.ResetConnections,
            s.SegmentsReceived,
            s.SegmentsSent,
            s.ErrorsReceived);

    private static UdpStatisticsSnapshot MapUdp(UdpStatistics s)
        => new(s.DatagramsReceived, s.DatagramsSent, s.IncomingDatagramsDiscarded, s.IncomingDatagramsWithErrors);

    private static void EnrichPids(List<NetworkConnection> rows)
    {
        IReadOnlyDictionary<(TransportProtocol, int, int), int>? map = null;
        try
        {
            map = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? NetworkWindowsTables.GetOwnerPids()
                : NetworkLinuxTables.GetOwnerPids();
        }
        catch (Exception)
        {
            return;
        }

        if (map is null || map.Count == 0)
            return;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (!map.TryGetValue((row.Protocol, row.LocalPort, row.RemotePort ?? 0), out var pid) || pid <= 0)
                continue;
            string? name = null;
            try
            {
                name = Process.GetProcessById(pid).ProcessName;
            }
            catch (Exception)
            {
            }

            rows[i] = row with { ProcessId = pid, ProcessName = name };
        }
    }
}
