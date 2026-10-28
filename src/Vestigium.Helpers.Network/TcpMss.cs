using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Vestigium.Helpers.Network;

internal static class TcpMss
{
    public static int? TryRead(Socket socket)
    {
        try
        {
            if (!socket.Connected)
                return null;
            return OperatingSystem.IsWindows() ? TryWindows(socket) : OperatingSystem.IsLinux() ? TryLinux(socket) : null;
        }
        catch
        {
            return null;
        }
    }

    private static int? TryWindows(Socket socket)
    {
        var buffer = new byte[Marshal.SizeOf<TcpInfoV0>()];
        var version = 0;
        var returned = socket.IOControl(SioTcpInfo, BitConverter.GetBytes(version), buffer);
        if (returned < 8)
            return null;
        var mss = BitConverter.ToUInt32(buffer, 4);
        return Sane(mss);
    }

    private static int? TryLinux(Socket socket)
    {
        var buffer = new byte[64];
        socket.GetSocketOption(SocketOptionLevel.Tcp, (SocketOptionName)TcpInfo, buffer);
        if (buffer.Length < 20)
            return null;
        var mss = BitConverter.ToUInt32(buffer, 16);
        return Sane(mss);
    }

    private static int? Sane(uint mss)
        => mss is >= 1 and <= 9000 ? (int)mss : null;

    private const int SioTcpInfo = unchecked((int)0xD8000027);
    private const int TcpInfo = 11;

    [StructLayout(LayoutKind.Sequential)]
    private struct TcpInfoV0
    {
        public uint State;
        public uint Mss;
        public ulong ConnectionTimeMs;
    }
}
