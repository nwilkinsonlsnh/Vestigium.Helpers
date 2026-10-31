using System.Text;

namespace Vestigium.Helpers.Network;

internal static class ServiceConfirm
{
    public static byte[]? ProbeFor(int port)
        => port is 80 or 8000 or 8008 or 8080 or 8081 or 8888
            ? Encoding.ASCII.GetBytes("GET / HTTP/1.0\r\n\r\n")
            : null;
}
