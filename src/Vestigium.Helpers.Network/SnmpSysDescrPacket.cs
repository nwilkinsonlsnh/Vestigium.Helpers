using System.Text;

namespace Vestigium.Helpers.Network;

internal static class SnmpSysDescrPacket
{
    private static readonly int[] SysDescrOid = [1, 3, 6, 1, 2, 1, 1, 1, 0];

    public static byte[] GetRequest(string community, int requestId)
    {
        var pdu = Sequence(0xA0,
            Integer(requestId),
            Integer(0),
            Integer(0),
            Sequence(0x30, Sequence(0x30, Oid(SysDescrOid), [0x05, 0x00])));
        return Sequence(0x30, Integer(1), OctetString(community), pdu);
    }

    public static string? ParseSysDescr(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 8)
            return null;
        var oid = EncodeOid(SysDescrOid);
        var index = IndexOf(packet, oid);
        if (index < 0)
            return null;
        var cursor = index + oid.Length;
        if (cursor + 2 >= packet.Length)
            return null;
        var tag = packet[cursor];
        if (tag is not (0x04 or 0x02))
            return null;
        var (length, next) = ReadLength(packet, cursor + 1);
        if (length < 0 || next + length > packet.Length)
            return null;
        return tag == 0x04
            ? Encoding.ASCII.GetString(packet.Slice(next, length)).Trim()
            : packet[next].ToString();
    }

    private static byte[] Sequence(byte tag, params byte[][] parts)
    {
        var body = Concat(parts);
        return Concat(new byte[] { tag }, Length(body.Length), body);
    }

    private static byte[] Integer(int value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        var start = 0;
        while (start < bytes.Length - 1 && bytes[start] == 0)
            start++;
        var body = bytes[start..];
        return Concat([0x02], Length(body.Length), body);
    }

    private static byte[] OctetString(string text)
    {
        var body = Encoding.ASCII.GetBytes(text);
        return Concat([0x04], Length(body.Length), body);
    }

    private static byte[] Oid(int[] arcs)
        => Concat([0x06], Length(EncodeOid(arcs).Length), EncodeOid(arcs));

    private static byte[] EncodeOid(int[] arcs)
    {
        var list = new List<byte> { (byte)(arcs[0] * 40 + arcs[1]) };
        for (var i = 2; i < arcs.Length; i++)
            list.AddRange(EncodeArc(arcs[i]));
        return list.ToArray();
    }

    private static byte[] EncodeArc(int value)
    {
        var stack = new Stack<byte>();
        stack.Push((byte)(value & 0x7F));
        value >>= 7;
        while (value > 0)
        {
            stack.Push((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }

        return stack.ToArray();
    }

    private static byte[] Length(int length)
    {
        if (length < 128)
            return [(byte)length];
        return [0x81, (byte)length];
    }

    private static (int Length, int Next) ReadLength(ReadOnlySpan<byte> packet, int index)
    {
        if (index >= packet.Length)
            return (-1, index);
        var first = packet[index];
        if (first < 128)
            return (first, index + 1);
        var count = first & 0x7F;
        if (count != 1 || index + 1 >= packet.Length)
            return (-1, index);
        return (packet[index + 1], index + 2);
    }

    private static int IndexOf(ReadOnlySpan<byte> packet, byte[] needle)
    {
        for (var i = 0; i <= packet.Length - needle.Length; i++)
        {
            if (packet.Slice(i, needle.Length).SequenceEqual(needle))
                return i;
        }

        return -1;
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var length = parts.Sum(static part => part.Length);
        var result = new byte[length];
        var offset = 0;
        foreach (var part in parts)
        {
            Buffer.BlockCopy(part, 0, result, offset, part.Length);
            offset += part.Length;
        }

        return result;
    }
}
