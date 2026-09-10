using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class DnsClient
{
    public static async Task<DnsLookupResult> LookupAsync(string name, DnsLookupOptions? options, CancellationToken token)
    {
        var qname = HelperGuard.NotBlank(name, nameof(name)).Trim();
        var o = options ?? new DnsLookupOptions();
        if (o.Port is < 1 or > 65535)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Dns, nameof(LookupAsync), $"Port={o.Port}");
            throw new ArgumentOutOfRangeException(nameof(o.Port), "Port must be 1–65535.");
        }

        if (o.Timeout < TimeSpan.FromMilliseconds(10) || o.Timeout > TimeSpan.FromSeconds(60))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Dns, nameof(LookupAsync), $"Timeout={o.Timeout}");
            throw new ArgumentOutOfRangeException(nameof(o.Timeout), "Timeout must be between 10 ms and 60 s.");
        }

        var question = PrepareQuestion(qname, o.Type);
        NetworkLog.Pending(
            HelperLog.Subcategories.Dns,
            $"lookup q={question} type={o.Type} server={o.Server ?? "os"}");

        var started = Stopwatch.StartNew();
        DnsLookupResult result;
        if (string.IsNullOrWhiteSpace(o.Server) && o.Type is DnsRecordType.A or DnsRecordType.Aaaa or DnsRecordType.Any)
            result = await OsLookupAsync(question, o, started, token).ConfigureAwait(false);
        else
            result = await WireLookupAsync(question, o, started, token).ConfigureAwait(false);

        NetworkLog.Success(
            HelperLog.Subcategories.Dns,
            $"{result.Rcode} q={result.Question} type={result.Type} server={result.Server ?? "os"} answers={result.Answers.Count} tcp={result.UsedTcp}");
        return result;
    }

    public static async Task<IReadOnlyList<DnsLookupResult>> LookupManyAsync(
        IEnumerable<string> names,
        DnsLookupOptions? options,
        CancellationToken token)
    {
        var list = names?.ToArray() ?? [];
        HelperGuard.NotEmpty(list, nameof(names));
        var results = new DnsLookupResult[list.Length];
        for (var i = 0; i < list.Length; i++)
            results[i] = await LookupAsync(list[i], options, token).ConfigureAwait(false);
        return results;
    }

    static string PrepareQuestion(string name, DnsRecordType type)
    {
        if (type != DnsRecordType.Ptr)
            return name.Trim().TrimEnd('.');

        if (IPAddress.TryParse(name, out var ip))
            return ip.AddressFamily == AddressFamily.InterNetworkV6
                ? ToIp6Arpa(ip)
                : string.Join('.', ip.GetAddressBytes().Reverse()) + ".in-addr.arpa";
        return name.Trim().TrimEnd('.');
    }

    static string ToIp6Arpa(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        var chars = new char[bytes.Length * 4]; // nibble + dot, last no extra handled below
        var parts = new List<string>(32);
        foreach (var b in bytes)
        {
            parts.Add((b & 0xF).ToString("x"));
            parts.Add((b >> 4).ToString("x"));
        }
        parts.Reverse();
        return string.Join('.', parts) + ".ip6.arpa";
    }

    static async Task<DnsLookupResult> OsLookupAsync(
        string question,
        DnsLookupOptions options,
        Stopwatch started,
        CancellationToken token)
    {
        try
        {
            var family = options.Type == DnsRecordType.Aaaa ? AddressFamily.InterNetworkV6
                : options.Type == DnsRecordType.A ? AddressFamily.InterNetwork
                : AddressFamily.Unspecified;
            var addresses = family == AddressFamily.Unspecified
                ? await Dns.GetHostAddressesAsync(question, token).ConfigureAwait(false)
                : await Dns.GetHostAddressesAsync(question, family, token).ConfigureAwait(false);
            var answers = addresses
                .Where(a => options.Type != DnsRecordType.A || a.AddressFamily == AddressFamily.InterNetwork)
                .Where(a => options.Type != DnsRecordType.Aaaa || a.AddressFamily == AddressFamily.InterNetworkV6)
                .Select(a => new DnsRecord(
                    a.AddressFamily == AddressFamily.InterNetworkV6 ? DnsRecordType.Aaaa : DnsRecordType.A,
                    question,
                    0,
                    a.ToString()))
                .ToArray();
            var rcode = answers.Length == 0 ? DnsRcode.NxDomain : DnsRcode.NoError;
            return new DnsLookupResult(question, options.Type, null, rcode, false, false, started.Elapsed, answers);
        }
        catch (SocketException)
        {
            return new DnsLookupResult(question, options.Type, null, DnsRcode.NxDomain, false, false, started.Elapsed, []);
        }
    }

    static async Task<DnsLookupResult> WireLookupAsync(
        string question,
        DnsLookupOptions options,
        Stopwatch started,
        CancellationToken token)
    {
        var server = options.Server;
        if (string.IsNullOrWhiteSpace(server))
            server = FirstOsDnsServer() ?? "127.0.0.1";

        if (!IPAddress.TryParse(server, out var serverIp))
        {
            try
            {
                var resolved = await Dns.GetHostAddressesAsync(server, token).ConfigureAwait(false);
                serverIp = resolved.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                    ?? resolved.FirstOrDefault();
            }
            catch (SocketException)
            {
                serverIp = null;
            }
        }

        if (serverIp is null)
            return new DnsLookupResult(question, options.Type, server, DnsRcode.Failed, false, false, started.Elapsed, []);

        var id = (ushort)Random.Shared.Next(1, ushort.MaxValue);
        var query = EncodeQuery(id, question, options.Type, options.RecursionDesired);
        try
        {
            var (udp, truncated) = await UdpExchangeAsync(serverIp, options.Port, query, options.Timeout, token).ConfigureAwait(false);
            var usedTcp = false;
            var message = udp;
            if (truncated || (udp.Length >= 4 && (udp[2] & 0x02) != 0))
            {
                message = await TcpExchangeAsync(serverIp, options.Port, query, options.Timeout, token).ConfigureAwait(false);
                usedTcp = true;
            }

            return Parse(question, options.Type, server, message, usedTcp, started.Elapsed, expectedId: id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TimeoutException)
        {
            return new DnsLookupResult(question, options.Type, server, DnsRcode.Timeout, false, false, started.Elapsed, []);
        }
        catch (SocketException ex) when (ex.SocketErrorCode is SocketError.ConnectionRefused or SocketError.TimedOut or SocketError.ConnectionReset)
        {
            var code = ex.SocketErrorCode == SocketError.ConnectionRefused ? DnsRcode.Refused : DnsRcode.Timeout;
            return new DnsLookupResult(question, options.Type, server, code, false, false, started.Elapsed, []);
        }
        catch (SocketException)
        {
            return new DnsLookupResult(question, options.Type, server, DnsRcode.Failed, false, false, started.Elapsed, []);
        }
    }

    static string? FirstOsDnsServer()
    {
        try
        {
            return NetworkInventoryEngine.Capture()
                .Adapters
                .SelectMany(a => a.DnsServers)
                .FirstOrDefault(s => IPAddress.TryParse(s, out var ip) && !IPAddress.IsLoopback(ip));
        }
        catch (NetworkInformationException)
        {
            return null;
        }
    }

    static byte[] EncodeQuery(ushort id, string qname, DnsRecordType type, bool rd)
    {
        using var ms = new MemoryStream();
        Span<byte> header = stackalloc byte[12];
        BinaryPrimitives.WriteUInt16BigEndian(header, id);
        BinaryPrimitives.WriteUInt16BigEndian(header[2..], (ushort)(rd ? 0x0100 : 0));
        BinaryPrimitives.WriteUInt16BigEndian(header[4..], 1);
        ms.Write(header);
        WriteName(ms, qname);
        Span<byte> tail = stackalloc byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(tail, (ushort)type);
        BinaryPrimitives.WriteUInt16BigEndian(tail[2..], 1);
        ms.Write(tail);
        return ms.ToArray();
    }

    static void WriteName(MemoryStream ms, string name)
    {
        foreach (var label in name.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var bytes = Encoding.ASCII.GetBytes(label);
            if (bytes.Length > 63)
                throw new ArgumentException("DNS label exceeds 63 octets.", nameof(name));
            ms.WriteByte((byte)bytes.Length);
            ms.Write(bytes);
        }
        ms.WriteByte(0);
    }

    static async Task<(byte[] Data, bool Truncated)> UdpExchangeAsync(
        IPAddress server,
        int port,
        byte[] query,
        TimeSpan timeout,
        CancellationToken token)
    {
        using var udp = new UdpClient(server.AddressFamily);
        udp.Client.ReceiveTimeout = (int)timeout.TotalMilliseconds;
        await udp.SendAsync(query, new IPEndPoint(server, port), token).ConfigureAwait(false);
        var receive = udp.ReceiveAsync(token).AsTask();
        var winner = await Task.WhenAny(receive, Task.Delay(timeout, token)).ConfigureAwait(false);
        if (winner != receive)
            throw new TimeoutException();
        var result = await receive.ConfigureAwait(false);
        return (result.Buffer, result.Buffer.Length >= 4 && (result.Buffer[2] & 0x02) != 0);
    }

    static async Task<byte[]> TcpExchangeAsync(
        IPAddress server,
        int port,
        byte[] query,
        TimeSpan timeout,
        CancellationToken token)
    {
        using var tcp = new TcpClient(server.AddressFamily);
        using var timed = CancellationTokenSource.CreateLinkedTokenSource(token);
        timed.CancelAfter(timeout);
        await tcp.ConnectAsync(server, port, timed.Token).ConfigureAwait(false);
        var stream = tcp.GetStream();
        var prefix = new byte[2 + query.Length];
        BinaryPrimitives.WriteUInt16BigEndian(prefix, (ushort)query.Length);
        Buffer.BlockCopy(query, 0, prefix, 2, query.Length);
        await stream.WriteAsync(prefix, timed.Token).ConfigureAwait(false);
        var lenBuf = new byte[2];
        await ReadExactAsync(stream, lenBuf, timed.Token).ConfigureAwait(false);
        var len = BinaryPrimitives.ReadUInt16BigEndian(lenBuf);
        var body = new byte[len];
        await ReadExactAsync(stream, body, timed.Token).ConfigureAwait(false);
        return body;
    }

    static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken token)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(read, buffer.Length - read), token).ConfigureAwait(false);
            if (n == 0)
                throw new EndOfStreamException();
            read += n;
        }
    }

    static DnsLookupResult Parse(
        string question,
        DnsRecordType type,
        string? server,
        byte[] message,
        bool usedTcp,
        TimeSpan elapsed,
        ushort expectedId)
    {
        if (message.Length < 12)
            return new DnsLookupResult(question, type, server, DnsRcode.FormErr, false, usedTcp, elapsed, []);

        var id = BinaryPrimitives.ReadUInt16BigEndian(message);
        if (id != expectedId)
            return new DnsLookupResult(question, type, server, DnsRcode.FormErr, false, usedTcp, elapsed, []);

        var flags = BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan(2));
        var truncated = (flags & 0x0200) != 0;
        var rcode = (DnsRcode)(flags & 0x000F);
        var qd = BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan(4));
        var an = BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan(6));
        var offset = 12;
        for (var i = 0; i < qd && offset < message.Length; i++)
            SkipName(message, ref offset, 0);
        if (offset + 4 <= message.Length && qd > 0)
            offset += 4;

        var answers = new List<DnsRecord>(an);
        for (var i = 0; i < an && offset < message.Length; i++)
        {
            if (!TryReadRecord(message, ref offset, out var record))
                break;
            answers.Add(record);
        }

        return new DnsLookupResult(question, type, server, rcode, truncated, usedTcp, elapsed, answers);
    }

    static bool TryReadRecord(byte[] message, ref int offset, out DnsRecord record)
    {
        record = new DnsRecord(DnsRecordType.A, "", 0, "");
        var name = ReadName(message, ref offset, 0);
        if (offset + 10 > message.Length)
            return false;
        var type = (DnsRecordType)BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan(offset));
        offset += 2;
        offset += 2; // class
        var ttl = BinaryPrimitives.ReadInt32BigEndian(message.AsSpan(offset));
        offset += 4;
        var rdlen = BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan(offset));
        offset += 2;
        if (offset + rdlen > message.Length)
            return false;
        var data = DecodeRdata(type, message, offset, rdlen);
        offset += rdlen;
        record = new DnsRecord(type, name, ttl, data);
        return true;
    }

    static string DecodeRdata(DnsRecordType type, byte[] message, int offset, int length)
    {
        try
        {
            return type switch
            {
                DnsRecordType.A when length == 4 => new IPAddress(message.AsSpan(offset, 4)).ToString(),
                DnsRecordType.Aaaa when length == 16 => new IPAddress(message.AsSpan(offset, 16)).ToString(),
                DnsRecordType.Ns or DnsRecordType.Cname or DnsRecordType.Ptr => ReadNameAt(message, offset),
                DnsRecordType.Mx when length >= 2 =>
                    BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan(offset)) + " " + ReadNameAt(message, offset + 2),
                DnsRecordType.Txt => DecodeTxt(message, offset, length),
                DnsRecordType.Soa => ReadNameAt(message, offset),
                DnsRecordType.Srv when length >= 6 =>
                    $"{BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan(offset))} {BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan(offset + 2))} {BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan(offset + 4))} {ReadNameAt(message, offset + 6)}",
                _ => Convert.ToHexString(message.AsSpan(offset, length))
            };
        }
        catch (Exception)
        {
            return Convert.ToHexString(message.AsSpan(offset, Math.Min(length, message.Length - offset)));
        }
    }

    static string DecodeTxt(byte[] message, int offset, int length)
    {
        var end = offset + length;
        var parts = new List<string>();
        var i = offset;
        while (i < end)
        {
            var len = message[i++];
            if (i + len > end)
                break;
            parts.Add(Encoding.UTF8.GetString(message, i, len));
            i += len;
        }
        return string.Join("", parts);
    }

    static string ReadNameAt(byte[] message, int offset)
    {
        var pos = offset;
        return ReadName(message, ref pos, 0);
    }

    static string ReadName(byte[] message, ref int offset, int depth)
    {
        if (depth > 10 || offset >= message.Length)
            return string.Empty;
        var labels = new List<string>();
        while (offset < message.Length)
        {
            var len = message[offset];
            if (len == 0)
            {
                offset++;
                break;
            }

            if ((len & 0xC0) == 0xC0)
            {
                if (offset + 1 >= message.Length)
                    break;
                var ptr = ((len & 0x3F) << 8) | message[offset + 1];
                offset += 2;
                var jumped = ptr;
                labels.Add(ReadName(message, ref jumped, depth + 1));
                break;
            }

            offset++;
            if (offset + len > message.Length)
                break;
            labels.Add(Encoding.ASCII.GetString(message, offset, len));
            offset += len;
        }

        return string.Join('.', labels.Where(l => l.Length > 0));
    }

    static void SkipName(byte[] message, ref int offset, int depth)
    {
        ReadName(message, ref offset, depth);
    }
}
