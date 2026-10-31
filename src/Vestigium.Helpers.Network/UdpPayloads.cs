namespace Vestigium.Helpers.Network;

internal static class UdpPayloads
{
    public static byte[]? For(int port)
        => port switch
        {
            53 => DnsQuery,
            123 => NtpRequest,
            _ => null
        };

    private static readonly byte[] DnsQuery =
    [
        0x00, 0x01,
        0x01, 0x00,
        0x00, 0x01,
        0x00, 0x00,
        0x00, 0x00,
        0x00, 0x00,
        0x00,
        0x00, 0x01,
        0x00, 0x01
    ];

    private static readonly byte[] NtpRequest = BuildNtp();

    private static byte[] BuildNtp()
    {
        var packet = new byte[48];
        packet[0] = 0x1B;
        return packet;
    }
}
