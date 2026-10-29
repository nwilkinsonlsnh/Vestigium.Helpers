using System.Net.Sockets;
using System.Text;

namespace Vestigium.Helpers.Network;

internal static class TcpBanner
{
    public static async Task<string?> TryReadAsync(Socket socket, int maxBytes, TimeSpan timeout, CancellationToken token)
    {
        if (!socket.Connected || maxBytes < 1)
            return null;

        var cap = Math.Clamp(maxBytes, 1, 1024);
        var buffer = new byte[cap];
        using var timed = CancellationTokenSource.CreateLinkedTokenSource(token);
        timed.CancelAfter(timeout);
        try
        {
            var read = await socket.ReceiveAsync(buffer.AsMemory(0, cap), SocketFlags.None, timed.Token).ConfigureAwait(false);
            if (read <= 0)
                return null;
            return Printable(buffer.AsSpan(0, read));
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    private static string? Printable(ReadOnlySpan<byte> bytes)
    {
        var text = Encoding.ASCII.GetString(bytes).Trim();
        if (text.Length == 0)
            return null;
        foreach (var ch in text)
        {
            if (ch is not (9 or 10 or 13) && (ch < 32 || ch > 126))
                return null;
        }

        return text;
    }
}
