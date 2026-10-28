using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Vestigium.Helpers.Network;

/// <summary>
/// Optional egress pin shared by echo, trace, and DNS.
/// Index 0 or omitted leaves the stack to choose. Index 0 is never rewritten to 1.
/// A source address, when set, must belong to the chosen interface.
/// </summary>
internal static class EgressBind
{
    internal const int UnicastIf = 31;
    internal const int SoBindToDevice = 25;

    public static bool IsPinned(int interfaceIndex, string? sourceAddress)
        => interfaceIndex >= 1 || !string.IsNullOrWhiteSpace(sourceAddress);

    public static void Validate(int interfaceIndex, string? sourceAddress)
    {
        if (interfaceIndex < 0)
        {
            NetworkLog.BindRejected(nameof(Validate), $"InterfaceIndex={interfaceIndex}");
            throw new ArgumentOutOfRangeException(nameof(interfaceIndex), "InterfaceIndex cannot be negative. Use 0 to let the stack choose.");
        }

        IPAddress? source = null;
        if (!string.IsNullOrWhiteSpace(sourceAddress))
        {
            if (!IPAddress.TryParse(sourceAddress.Trim(), out source))
            {
                NetworkLog.BindRejected(nameof(Validate), "SourceAddress is not an IP");
                throw new ArgumentException("SourceAddress must be an IPv4 or IPv6 address.", nameof(sourceAddress));
            }
        }

        if (interfaceIndex == 0)
        {
            if (source is null)
                return;
            if (!AddressLivesOnAnyAdapter(source))
            {
                NetworkLog.BindRejected(nameof(Validate), "SourceAddress is not assigned on this host");
                throw new ArgumentException("SourceAddress must belong to a local adapter.", nameof(sourceAddress));
            }

            return;
        }

        var nic = FindAdapter(interfaceIndex);
        if (nic is null)
        {
            NetworkLog.BindRejected(nameof(Validate), $"InterfaceIndex={interfaceIndex} is not present");
            throw new ArgumentOutOfRangeException(nameof(interfaceIndex), "InterfaceIndex does not match a local adapter.");
        }

        if (source is not null && !AddressLivesOn(nic, source))
        {
            NetworkLog.BindRejected(nameof(Validate), "SourceAddress is not assigned on the chosen adapter");
            throw new ArgumentException("SourceAddress must belong to the chosen interface.", nameof(sourceAddress));
        }
    }

    public static void Apply(Socket socket, int interfaceIndex, string? sourceAddress)
    {
        Validate(interfaceIndex, sourceAddress);
        if (!IsPinned(interfaceIndex, sourceAddress))
            return;

        if (!string.IsNullOrWhiteSpace(sourceAddress) && IPAddress.TryParse(sourceAddress.Trim(), out var source))
            socket.Bind(new IPEndPoint(source, 0));

        if (interfaceIndex < 1)
            return;

        var nic = FindAdapter(interfaceIndex);
        if (nic is null)
            return;

        if (OperatingSystem.IsWindows())
        {
            var family = socket.AddressFamily;
            var level = family == AddressFamily.InterNetworkV6 ? SocketOptionLevel.IPv6 : SocketOptionLevel.IP;
            var packed = IPAddress.HostToNetworkOrder(interfaceIndex);
            socket.SetSocketOption(level, (SocketOptionName)UnicastIf, packed);
            return;
        }

        var name = Encoding.ASCII.GetBytes(nic.Name + "\0");
        socket.SetSocketOption(SocketOptionLevel.Socket, (SocketOptionName)SoBindToDevice, name);
    }

    internal static NetworkInterface? FindAdapter(int index)
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            try
            {
                var v4 = nic.GetIPProperties().GetIPv4Properties();
                if (v4 is not null && v4.Index == index)
                    return nic;
            }
            catch (NetworkInformationException)
            {
            }

            try
            {
                var v6 = nic.GetIPProperties().GetIPv6Properties();
                if (v6 is not null && v6.Index == index)
                    return nic;
            }
            catch (NetworkInformationException)
            {
            }
        }

        return null;
    }

    internal static bool AddressLivesOnAnyAdapter(IPAddress source)
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (AddressLivesOn(nic, source))
                return true;
        }

        return false;
    }

    internal static bool AddressLivesOn(NetworkInterface nic, IPAddress source)
    {
        foreach (var uni in nic.GetIPProperties().UnicastAddresses)
        {
            if (uni.Address.Equals(source))
                return true;
            if (source.IsIPv4MappedToIPv6 && uni.Address.Equals(source.MapToIPv4()))
                return true;
            if (uni.Address.IsIPv4MappedToIPv6 && uni.Address.MapToIPv4().Equals(source))
                return true;
        }

        return false;
    }
}
