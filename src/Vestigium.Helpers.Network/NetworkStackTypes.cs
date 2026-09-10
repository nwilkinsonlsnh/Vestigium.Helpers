using System.Net.Sockets;

namespace Vestigium.Helpers.Network;

public enum TransportProtocol
{
    Tcp,
    Udp
}

public enum RouteFamily
{
    All,
    IPv4,
    IPv6
}

public sealed record NetworkConnectionQuery(
    TransportProtocol? Protocol = null,
    bool ListeningOnly = false,
    bool EstablishedOnly = false);

public sealed record NetworkConnection(
    TransportProtocol Protocol,
    AddressFamily Family,
    string LocalAddress,
    int LocalPort,
    string? RemoteAddress,
    int? RemotePort,
    string? State,
    int? ProcessId,
    string? ProcessName);

public sealed record TcpStatisticsSnapshot(
    long ConnectionsAccepted,
    long ConnectionsInitiated,
    long CumulativeConnections,
    long CurrentConnections,
    long FailedConnectionAttempts,
    long ResetConnections,
    long SegmentsReceived,
    long SegmentsSent,
    long ErrorsReceived);

public sealed record UdpStatisticsSnapshot(
    long DatagramsReceived,
    long DatagramsSent,
    long IncomingDatagramsDiscarded,
    long IncomingDatagramsWithErrors);

public sealed record NetworkStackStatistics(
    TcpStatisticsSnapshot? TcpIPv4,
    TcpStatisticsSnapshot? TcpIPv6,
    UdpStatisticsSnapshot? UdpIPv4,
    UdpStatisticsSnapshot? UdpIPv6);

public sealed record NetworkRoute(
    AddressFamily Family,
    string Destination,
    int PrefixLength,
    string? Mask,
    string Gateway,
    string? InterfaceName,
    int? InterfaceIndex,
    int Metric,
    bool IsPersistent,
    string? Protocol);

public sealed record NetworkNeighbor(
    AddressFamily Family,
    string Address,
    string? MacAddress,
    string? InterfaceName,
    string State);
