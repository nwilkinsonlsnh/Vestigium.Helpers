using System.Net;
using System.Net.Sockets;

namespace Vestigium.Helpers.Watch.Dns;

public static class PacketWatch
{
    public const int DnsPort = 53;

    public static bool Requested(string[] args)
        => args.Any(arg => string.Equals(arg, "packet", StringComparison.OrdinalIgnoreCase));

    public static WatchRow Read(byte[]? payload)
    {
        if (!DnsMessage.TryRead(payload, out var name, out var type, out var response, out var status, out var answers))
            return new WatchRow(DateTimeOffset.UtcNow, "", 0, "", "", "Not a question", "", "packet");

        return new WatchRow(DateTimeOffset.UtcNow, "", 0, name, type, response ? status : "", response ? answers : "", "packet")
        {
            ResolverCount = response ? 1 : 1,
            PacketCount = response ? 1 : 0,
            Total = response ? 2 : 1
        };
    }

    public static Task<int> RunAsync(WatchPipe pipe, WatchClock clock, CancellationToken token)
        => RunAsync(pipe, new WatchRollup(), clock, token);

    public static async Task<int> RunAsync(WatchPipe pipe, WatchRollup rollup, WatchClock clock, CancellationToken token)
    {
        UdpClient? udp = null;
        TcpListener? tcp = null;
        try
        {
            udp = new UdpClient(DnsPort);
            tcp = new TcpListener(IPAddress.Any, DnsPort);
            tcp.Start();
            var outbound = ReadOutboundAsync(pipe, rollup, token);
            var udpTask = ReadUdpAsync(pipe, rollup, udp, token);
            var tcpTask = ReadTcpAsync(pipe, rollup, tcp, token);
            await Task.WhenAny(clock.Completion, outbound, udpTask, tcpTask).ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex)
        {
            var message = string.IsNullOrWhiteSpace(ex.Message) ? "Bind failed" : ex.Message.Trim();
            await pipe.WriteAsync(
                new WatchRow(DateTimeOffset.UtcNow, "", 0, "", "", "Failed", message, "packet"),
                CancellationToken.None).ConfigureAwait(false);
            return ResolverWatch.SessionFailed;
        }
        finally
        {
            udp?.Dispose();
            tcp?.Stop();
        }
    }

    private static async Task ReadOutboundAsync(WatchPipe pipe, WatchRollup rollup, CancellationToken token)
    {
        var sockets = OutboundFrame.Open();
        if (sockets.Count == 0)
        {
            await pipe.WriteAsync(
                new WatchRow(DateTimeOffset.UtcNow, "", 0, "", "", "Unseen", "Outbound port 53 capture did not open.", "packet"),
                CancellationToken.None).ConfigureAwait(false);
            return;
        }

        try
        {
            var tasks = sockets.Select(socket => ReadRawAsync(pipe, rollup, socket, token));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        finally
        {
            foreach (var socket in sockets)
                socket.Dispose();
        }
    }

    private static async Task ReadRawAsync(WatchPipe pipe, WatchRollup rollup, Socket socket, CancellationToken token)
    {
        var buffer = new byte[65535];
        while (!token.IsCancellationRequested)
        {
            var read = await socket.ReceiveAsync(buffer, SocketFlags.None, token).ConfigureAwait(false);
            if (!OutboundFrame.TryRead(buffer[..read], out var question))
                continue;
            await WriteMessageAsync(pipe, rollup, question, token).ConfigureAwait(false);
        }
    }

    private static async Task ReadUdpAsync(WatchPipe pipe, WatchRollup rollup, UdpClient udp, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var result = await udp.ReceiveAsync(token).ConfigureAwait(false);
            await WriteMessageAsync(pipe, rollup, result.Buffer, token).ConfigureAwait(false);
        }
    }

    private static async Task ReadTcpAsync(WatchPipe pipe, WatchRollup rollup, TcpListener tcp, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            using var client = await tcp.AcceptTcpClientAsync(token).ConfigureAwait(false);
            var stream = client.GetStream();
            var lengthBytes = new byte[2];
            if (await stream.ReadAsync(lengthBytes, token).ConfigureAwait(false) < 2)
                continue;
            var length = (lengthBytes[0] << 8) | lengthBytes[1];
            var payload = new byte[length];
            if (await stream.ReadAsync(payload, token).ConfigureAwait(false) < length)
                continue;
            await WriteMessageAsync(pipe, rollup, payload, token).ConfigureAwait(false);
        }
    }

    private static async Task WriteMessageAsync(WatchPipe pipe, WatchRollup rollup, byte[] payload, CancellationToken token)
    {
        if (!DnsMessage.TryRead(payload, out var name, out var type, out var response, out var status, out var answers))
            return;

        var row = rollup.Add(name, type, WatchSource.Port, 0, response ? status : null, response ? answers : null, response);
        if (row is not null)
            await pipe.WriteAsync(row, token).ConfigureAwait(false);
    }
}
