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
        if (!DnsQuestion.TryRead(payload, out var name, out var type))
            return new WatchRow(DateTimeOffset.UtcNow, "", 0, "", "", "Not a question", "", "packet");

        return new WatchRow(DateTimeOffset.UtcNow, "", 0, name, type, "", "", "packet");
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
            await WriteQuestionAsync(pipe, rollup, question, token).ConfigureAwait(false);
        }
    }

    private static async Task ReadUdpAsync(WatchPipe pipe, WatchRollup rollup, UdpClient udp, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var result = await udp.ReceiveAsync(token).ConfigureAwait(false);
            await WriteQuestionAsync(pipe, rollup, result.Buffer, token).ConfigureAwait(false);
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
            await WriteQuestionAsync(pipe, rollup, payload, token).ConfigureAwait(false);
        }
    }

    private static async Task WriteQuestionAsync(WatchPipe pipe, WatchRollup rollup, byte[] payload, CancellationToken token)
    {
        var parsed = Read(payload);
        if (parsed.Status == "Not a question")
            return;

        var row = rollup.Add(parsed.Name, parsed.Type, WatchSource.Port, 0);
        if (row is not null)
            await pipe.WriteAsync(row, token).ConfigureAwait(false);
    }
}

public static class DnsQuestion
{
    public static bool TryRead(byte[]? payload, out string name, out string type)
    {
        name = "";
        type = "";
        if (payload is null || payload.Length < 12)
            return false;
        if ((payload[2] & 0x80) != 0)
            return false;
        if (payload[4] == 0 && payload[5] == 0)
            return false;

        var at = 12;
        var labels = new List<string>();
        while (at < payload.Length)
        {
            var length = payload[at++];
            if (length == 0)
                break;
            if ((length & 0xC0) == 0xC0 || at + length > payload.Length)
                return false;
            labels.Add(System.Text.Encoding.ASCII.GetString(payload, at, length));
            at += length;
        }

        if (labels.Count == 0 || at + 2 > payload.Length)
            return false;

        name = string.Join(".", labels);
        type = ((payload[at] << 8) | payload[at + 1]).ToString();
        return true;
    }
}
