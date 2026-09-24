using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class BoundIcmpEcho
{
    public static async Task<IcmpEchoReply> SendAsync(
        string target,
        byte[] buffer,
        int timeoutMs,
        IcmpEchoOptions options,
        int sequence,
        CancellationToken token)
    {
        IPAddress dest;
        try
        {
            dest = await ResolveAsync(target, options.SourceAddress, token).ConfigureAwait(false);
        }
        catch (SocketException ex)
        {
            return new IcmpEchoReply(sequence, IcmpEchoStatus.Failed, null, 0, 0, false, ex.Message);
        }

        var family = dest.AddressFamily;
        var icmp = family == AddressFamily.InterNetworkV6 ? ProtocolType.IcmpV6 : ProtocolType.Icmp;
        try
        {
            using var socket = CreateSocket(family, icmp);
            EgressBind.Apply(socket, options.InterfaceIndex, options.SourceAddress);
            socket.Ttl = (short)Math.Clamp(options.Ttl, 1, 255);
            if (family == AddressFamily.InterNetwork)
                socket.DontFragment = options.DontFragment;

            var id = (ushort)(Environment.ProcessId & 0xFFFF);
            var seq = (ushort)sequence;
            var packet = BuildEcho(family, id, seq, buffer);
            var started = Stopwatch.StartNew();
            await socket.SendToAsync(packet, SocketFlags.None, new IPEndPoint(dest, 0), token).ConfigureAwait(false);

            var receive = new byte[Math.Max(packet.Length + 64, 128)];
            using var timed = CancellationTokenSource.CreateLinkedTokenSource(token);
            timed.CancelAfter(timeoutMs);
            try
            {
                var got = await socket.ReceiveFromAsync(receive, SocketFlags.None, new IPEndPoint(family == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0), timed.Token).ConfigureAwait(false);
                var rtt = started.ElapsedMilliseconds;
                var remote = got.RemoteEndPoint as IPEndPoint;
                return ParseReply(receive.AsSpan(0, got.Count), family, id, seq, sequence, rtt, remote?.Address);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                return new IcmpEchoReply(sequence, IcmpEchoStatus.TimedOut, dest.ToString(), started.ElapsedMilliseconds, 0, false, "timeout");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SocketException ex) when (IcmpEchoEngine.IsForbidden(ex))
        {
            return new IcmpEchoReply(sequence, IcmpEchoStatus.ProtocolForbidden, dest.ToString(), 0, 0, false, ex.Message);
        }
        catch (SocketException ex)
        {
            return new IcmpEchoReply(sequence, IcmpEchoStatus.Failed, dest.ToString(), 0, 0, false, ex.Message);
        }
        catch (PlatformNotSupportedException ex)
        {
            return new IcmpEchoReply(sequence, IcmpEchoStatus.ProtocolForbidden, dest.ToString(), 0, 0, true, ex.Message);
        }
    }

    private static Socket CreateSocket(AddressFamily family, ProtocolType icmp)
    {
        try
        {
            return new Socket(family, SocketType.Dgram, icmp);
        }
        catch (SocketException)
        {
            return new Socket(family, SocketType.Raw, icmp);
        }
    }

    private static async Task<IPAddress> ResolveAsync(string target, string? sourceAddress, CancellationToken token)
    {
        AddressFamily? pin = null;
        if (!string.IsNullOrWhiteSpace(sourceAddress) && IPAddress.TryParse(sourceAddress.Trim(), out var source))
            pin = source.AddressFamily;

        if (IPAddress.TryParse(target, out var parsed))
        {
            if (pin is { } required && parsed.AddressFamily != required)
                throw new SocketException((int)SocketError.AddressFamilyNotSupported);
            return parsed;
        }

        var addrs = pin is { } family
            ? await Dns.GetHostAddressesAsync(target, family, token).ConfigureAwait(false)
            : await Dns.GetHostAddressesAsync(target, token).ConfigureAwait(false);
        var dest = pin is { } want
            ? addrs.FirstOrDefault(a => a.AddressFamily == want)
            : addrs.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) ?? addrs.FirstOrDefault();
        return dest ?? throw new SocketException((int)SocketError.HostNotFound);
    }

    private static byte[] BuildEcho(AddressFamily family, ushort id, ushort seq, byte[] payload)
    {
        var type = family == AddressFamily.InterNetworkV6 ? (byte)128 : (byte)8;
        var packet = new byte[8 + payload.Length];
        packet[0] = type;
        packet[1] = 0;
        BinaryWriteU16(packet, 4, id);
        BinaryWriteU16(packet, 6, seq);
        if (payload.Length > 0)
            Buffer.BlockCopy(payload, 0, packet, 8, payload.Length);
        var sum = Checksum(packet);
        packet[2] = (byte)(sum >> 8);
        packet[3] = (byte)sum;
        return packet;
    }

    private static IcmpEchoReply ParseReply(ReadOnlySpan<byte> buffer, AddressFamily family, ushort id, ushort seq, int sequence, long rtt, IPAddress? remote)
    {
        var icmp = family == AddressFamily.InterNetwork ? SkipIpv4(buffer) : buffer;
        if (icmp.Length < 8)
            return new IcmpEchoReply(sequence, IcmpEchoStatus.Failed, IcmpEchoEngine.MapAddress(remote), rtt, 0, false, "short");

        var type = icmp[0];
        var echoReply = family == AddressFamily.InterNetworkV6 ? (byte)129 : (byte)0;
        var timeExceeded = family == AddressFamily.InterNetworkV6 ? (byte)3 : (byte)11;
        var unreachable = family == AddressFamily.InterNetworkV6 ? (byte)1 : (byte)3;
        var packetTooBig = family == AddressFamily.InterNetworkV6 ? (byte)2 : (byte)3;

        if (type == echoReply)
        {
            var gotId = BinaryReadU16(icmp, 4);
            var gotSeq = BinaryReadU16(icmp, 6);
            if (gotId != id || gotSeq != seq)
                return new IcmpEchoReply(sequence, IcmpEchoStatus.Failed, IcmpEchoEngine.MapAddress(remote), rtt, 0, false, "mismatch");
            return new IcmpEchoReply(sequence, IcmpEchoStatus.Success, IcmpEchoEngine.MapAddress(remote), rtt, 0, false, null);
        }

        if (type == timeExceeded)
            return new IcmpEchoReply(sequence, IcmpEchoStatus.TtlExpired, IcmpEchoEngine.MapAddress(remote), rtt, 0, false, "ttl");
        if (type == unreachable || type == packetTooBig)
            return new IcmpEchoReply(sequence, IcmpEchoStatus.DestinationUnreachable, IcmpEchoEngine.MapAddress(remote), rtt, 0, false, "unreach");
        return new IcmpEchoReply(sequence, IcmpEchoStatus.Failed, IcmpEchoEngine.MapAddress(remote), rtt, 0, false, $"type={type}");
    }

    private static ReadOnlySpan<byte> SkipIpv4(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 20 || (buffer[0] >> 4) != 4)
            return buffer;
        var header = (buffer[0] & 0x0F) * 4;
        return header < buffer.Length ? buffer[header..] : buffer;
    }

    private static void BinaryWriteU16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)(value >> 8);
        buffer[offset + 1] = (byte)value;
    }

    private static ushort BinaryReadU16(ReadOnlySpan<byte> buffer, int offset)
        => (ushort)((buffer[offset] << 8) | buffer[offset + 1]);

    private static ushort Checksum(ReadOnlySpan<byte> buffer)
    {
        uint sum = 0;
        for (var i = 0; i + 1 < buffer.Length; i += 2)
            sum += (uint)((buffer[i] << 8) | buffer[i + 1]);
        if ((buffer.Length & 1) != 0)
            sum += (uint)(buffer[^1] << 8);
        while (sum >> 16 != 0)
            sum = (sum & 0xFFFF) + (sum >> 16);
        return (ushort)~sum;
    }
}
