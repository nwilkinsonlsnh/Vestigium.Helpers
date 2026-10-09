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

    public static async Task<int> RunAsync(WatchPipe pipe, WatchClock clock, CancellationToken token)
    {
        UdpClient? udp = null;
        TcpListener? tcp = null;
        try
        {
            udp = new UdpClient(DnsPort);
            tcp = new TcpListener(IPAddress.Any, DnsPort);
            tcp.Start();
            var udpTask = ReadUdpAsync(pipe, udp, token);
            var tcpTask = ReadTcpAsync(pipe, tcp, token);
            await Task.WhenAny(clock.Completion, udpTask, tcpTask).ConfigureAwait(false);
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

    private static async Task ReadUdpAsync(WatchPipe pipe, UdpClient udp, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var result = await udp.ReceiveAsync(token).ConfigureAwait(false);
            await pipe.WriteAsync(Read(result.Buffer), token).ConfigureAwait(false);
        }
    }

    private static async Task ReadTcpAsync(WatchPipe pipe, TcpListener tcp, CancellationToken token)
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
            await pipe.WriteAsync(Read(payload), token).ConfigureAwait(false);
        }
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
