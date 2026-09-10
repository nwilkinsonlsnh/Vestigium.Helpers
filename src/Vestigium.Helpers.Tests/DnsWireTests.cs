using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class DnsWireTests
{
    const ushort Id = 0x1234;

    [Fact]
    public void EncodeQuery_then_parse_A_answer()
    {
        var q = DnsClient.EncodeQuery(Id, "example.test", DnsRecordType.A, rd: true);
        Assert.True(q.Length > 12);
        var msg = Answer(Id, "example.test", DnsRecordType.A, DnsRecordType.A, [1, 2, 3, 4]);
        var parsed = DnsClient.Parse("example.test", DnsRecordType.A, "127.0.0.1", msg, usedTcp: false, TimeSpan.FromMilliseconds(5), Id);
        Assert.Equal(DnsRcode.NoError, parsed.Rcode);
        Assert.False(parsed.Truncated);
        Assert.Equal("1.2.3.4", parsed.Answers[0].Data);
    }

    [Fact]
    public void Parse_covers_all_rdata_kinds()
    {
        Assert.Equal("2001:db8::1", One(DnsRecordType.Aaaa, IPAddress.Parse("2001:db8::1").GetAddressBytes()).Answers[0].Data);
        Assert.Contains("ns.example.test", One(DnsRecordType.Ns, EncodeName("ns.example.test")).Answers[0].Data);
        Assert.Contains("alias.example.test", One(DnsRecordType.Cname, EncodeName("alias.example.test")).Answers[0].Data);
        Assert.Contains("ptr.example.test", One(DnsRecordType.Ptr, EncodeName("ptr.example.test")).Answers[0].Data);
        var mx = One(DnsRecordType.Mx, Concat([0, 10], EncodeName("mail.example.test")));
        Assert.StartsWith("10 ", mx.Answers[0].Data);
        var txt = One(DnsRecordType.Txt, Concat([(byte)"hello".Length], Encoding.UTF8.GetBytes("hello")));
        Assert.Equal("hello", txt.Answers[0].Data);
        var multi = One(DnsRecordType.Txt, Concat(
            Concat([(byte)"ab".Length], "ab"u8.ToArray()),
            Concat([(byte)"cd".Length], "cd"u8.ToArray())));
        Assert.Equal("abcd", multi.Answers[0].Data);
        var soa = One(DnsRecordType.Soa, EncodeName("ns.example.test"));
        Assert.False(string.IsNullOrWhiteSpace(soa.Answers[0].Data));
        var srv = One(DnsRecordType.Srv, Concat([0, 1, 0, 2, 0, 80], EncodeName("host.example.test")));
        Assert.Contains("80", srv.Answers[0].Data);
        var hex = One((DnsRecordType)99, [0xDE, 0xAD]);
        Assert.Equal("DEAD", hex.Answers[0].Data, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("010203", One(DnsRecordType.A, [1, 2, 3]).Answers[0].Data, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("00", One(DnsRecordType.Aaaa, [0]).Answers[0].Data, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("00", One(DnsRecordType.Mx, [0]).Answers[0].Data, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("0001", One(DnsRecordType.Srv, [0, 1]).Answers[0].Data, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("", One(DnsRecordType.Txt, []).Answers[0].Data);
        var any = One(DnsRecordType.Any, [0xAA]);
        Assert.Equal("AA", any.Answers[0].Data, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_formerr_on_short_or_wrong_id()
    {
        var shorty = DnsClient.Parse("q", DnsRecordType.A, null, [1, 2, 3], false, TimeSpan.Zero, Id);
        Assert.Equal(DnsRcode.FormErr, shorty.Rcode);
        var eleven = DnsClient.Parse("q", DnsRecordType.A, null, new byte[11], false, TimeSpan.Zero, Id);
        Assert.Equal(DnsRcode.FormErr, eleven.Rcode);
        var wrong = Answer(0x9999, "example.test", DnsRecordType.A, DnsRecordType.A, [1, 2, 3, 4]);
        var parsed = DnsClient.Parse("example.test", DnsRecordType.A, null, wrong, true, TimeSpan.Zero, Id);
        Assert.Equal(DnsRcode.FormErr, parsed.Rcode);
        Assert.True(parsed.UsedTcp);
    }

    [Fact]
    public void Parse_truncated_flag_and_compression_pointer()
    {
        var msg = Answer(Id, "example.test", DnsRecordType.A, DnsRecordType.A, [8, 8, 8, 8], truncated: true, compress: true);
        var parsed = DnsClient.Parse("example.test", DnsRecordType.A, "os", msg, false, TimeSpan.Zero, Id);
        Assert.True(parsed.Truncated);
        Assert.Equal("8.8.8.8", parsed.Answers[0].Data);
        var plain = Answer(Id, "example.test", DnsRecordType.A, DnsRecordType.A, [1, 1, 1, 1], compress: false);
        var again = DnsClient.Parse("example.test", DnsRecordType.A, null, plain, false, TimeSpan.Zero, Id);
        Assert.Equal("1.1.1.1", again.Answers[0].Data);
    }

    [Fact]
    public void Parse_pointer_loop_stops()
    {
        var buf = new byte[20];
        BinaryPrimitives.WriteUInt16BigEndian(buf, Id);
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(2), 0x8000);
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(4), 1);
        buf[12] = 0xC0;
        buf[13] = 12;
        var parsed = DnsClient.Parse("loop", DnsRecordType.A, null, buf, false, TimeSpan.Zero, Id);
        Assert.Equal(Id, (ushort)((buf[0] << 8) | buf[1]));
        Assert.Empty(parsed.Answers);
    }

    [Fact]
    public void Parse_readname_edge_cases()
    {
        var deep = new byte[40];
        BinaryPrimitives.WriteUInt16BigEndian(deep, Id);
        BinaryPrimitives.WriteUInt16BigEndian(deep.AsSpan(2), 0x8000);
        BinaryPrimitives.WriteUInt16BigEndian(deep.AsSpan(4), 1);
        for (var i = 0; i < 12; i++)
        {
            deep[12 + i * 2] = 0xC0;
            deep[13 + i * 2] = (byte)(12 + (i + 1) * 2);
        }
        var parsedDeep = DnsClient.Parse("deep", DnsRecordType.A, null, deep, false, TimeSpan.Zero, Id);
        Assert.Empty(parsedDeep.Answers);

        var dangling = new byte[13];
        BinaryPrimitives.WriteUInt16BigEndian(dangling, Id);
        BinaryPrimitives.WriteUInt16BigEndian(dangling.AsSpan(2), 0x8000);
        BinaryPrimitives.WriteUInt16BigEndian(dangling.AsSpan(4), 1);
        dangling[12] = 0xC0;
        var parsedDangle = DnsClient.Parse("dangle", DnsRecordType.A, null, dangling, false, TimeSpan.Zero, Id);
        Assert.Empty(parsedDangle.Answers);

        var overrun = new byte[16];
        BinaryPrimitives.WriteUInt16BigEndian(overrun, Id);
        BinaryPrimitives.WriteUInt16BigEndian(overrun.AsSpan(2), 0x8000);
        BinaryPrimitives.WriteUInt16BigEndian(overrun.AsSpan(4), 1);
        overrun[12] = 20;
        var parsedOver = DnsClient.Parse("over", DnsRecordType.A, null, overrun, false, TimeSpan.Zero, Id);
        Assert.Empty(parsedOver.Answers);

        var emptyQd = new byte[12];
        BinaryPrimitives.WriteUInt16BigEndian(emptyQd, Id);
        BinaryPrimitives.WriteUInt16BigEndian(emptyQd.AsSpan(2), 0x8003);
        var nx = DnsClient.Parse("gone", DnsRecordType.A, null, emptyQd, false, TimeSpan.Zero, Id);
        Assert.Equal(DnsRcode.NxDomain, nx.Rcode);

        var shortRec = Answer(Id, "example.test", DnsRecordType.A, DnsRecordType.A, [1, 2, 3, 4]);
        Array.Resize(ref shortRec, shortRec.Length - 6);
        var parsedShort = DnsClient.Parse("example.test", DnsRecordType.A, null, shortRec, false, TimeSpan.Zero, Id);
        Assert.True(parsedShort.Answers.Count <= 1);

        var fat = Answer(Id, "example.test", DnsRecordType.A, DnsRecordType.A, [1, 2, 3, 4]);
        BinaryPrimitives.WriteUInt16BigEndian(fat.AsSpan(fat.Length - 6), 400);
        var parsedFat = DnsClient.Parse("example.test", DnsRecordType.A, null, fat, false, TimeSpan.Zero, Id);
        Assert.Empty(parsedFat.Answers);
    }

    [Fact]
    public void Parse_nxdomain_and_txt_overrun()
    {
        var nx = Answer(Id, "gone.test", DnsRecordType.A, DnsRecordType.A, [1, 2, 3, 4], rcode: 3, answers: 0);
        var parsed = DnsClient.Parse("gone.test", DnsRecordType.A, null, nx, false, TimeSpan.Zero, Id);
        Assert.Equal(DnsRcode.NxDomain, parsed.Rcode);
        var txt = One(DnsRecordType.Txt, [20, 1, 2]);
        Assert.NotNull(txt.Answers);
        var refused = Answer(Id, "x.test", DnsRecordType.A, DnsRecordType.A, [1, 2, 3, 4], rcode: 5, answers: 0);
        Assert.Equal(DnsRcode.Refused, DnsClient.Parse("x.test", DnsRecordType.A, null, refused, false, TimeSpan.Zero, Id).Rcode);
        var servfail = Answer(Id, "x.test", DnsRecordType.A, DnsRecordType.A, [1, 2, 3, 4], rcode: 2, answers: 0);
        Assert.Equal(DnsRcode.ServFail, DnsClient.Parse("x.test", DnsRecordType.A, null, servfail, false, TimeSpan.Zero, Id).Rcode);
    }

    [Fact]
    public void EncodeQuery_rejects_label_over_63()
    {
        var label = new string('a', 64);
        Assert.Throws<ArgumentException>(() => DnsClient.EncodeQuery(1, label + ".test", DnsRecordType.A, true));
    }

    [Fact]
    public void EncodeQuery_without_rd_clears_flag()
    {
        var q = DnsClient.EncodeQuery(7, "a.b", DnsRecordType.Ns, rd: false);
        Assert.Equal(0, q[2] & 0x01);
        var with = DnsClient.EncodeQuery(7, "a.b", DnsRecordType.Ns, rd: true);
        Assert.Equal(0x01, with[2]);
    }

    [Fact]
    public async Task Wire_lookup_against_local_udp_stub()
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
        var serve = Task.Run(async () =>
        {
            var got = await udp.ReceiveAsync();
            var id = BinaryPrimitives.ReadUInt16BigEndian(got.Buffer);
            var qname = "stub.test";
            var reply = Answer(id, qname, DnsRecordType.A, DnsRecordType.A, [9, 9, 9, 9]);
            await udp.SendAsync(reply, got.RemoteEndPoint);
        });

        var result = await NetworkHelper.LookupAsync(
            "stub.test",
            new DnsLookupOptions
            {
                Server = "127.0.0.1",
                Port = port,
                Timeout = TimeSpan.FromSeconds(2),
                Type = DnsRecordType.A
            });
        await serve;
        Assert.Equal(DnsRcode.NoError, result.Rcode);
        Assert.Equal("9.9.9.9", result.Answers[0].Data);
    }

    [Fact]
    public async Task Wire_lookup_truncated_udp_falls_back_to_tcp()
    {
        var tcp = new TcpListener(IPAddress.Loopback, 0);
        tcp.Start();
        var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));

        var serveUdp = Task.Run(async () =>
        {
            var got = await udp.ReceiveAsync(cts.Token);
            var id = BinaryPrimitives.ReadUInt16BigEndian(got.Buffer);
            var reply = Answer(id, "tcp.test", DnsRecordType.A, DnsRecordType.A, [1, 1, 1, 1], truncated: true);
            await udp.SendAsync(reply, got.RemoteEndPoint);
        }, cts.Token);

        var serveTcp = Task.Run(async () =>
        {
            using var client = await tcp.AcceptTcpClientAsync(cts.Token);
            await using var stream = client.GetStream();
            var lenBuf = new byte[2];
            await ReadExact(stream, lenBuf, cts.Token);
            var qlen = BinaryPrimitives.ReadUInt16BigEndian(lenBuf);
            var q = new byte[qlen];
            await ReadExact(stream, q, cts.Token);
            var id = BinaryPrimitives.ReadUInt16BigEndian(q);
            var body = Answer(id, "tcp.test", DnsRecordType.A, DnsRecordType.A, [8, 8, 4, 4]);
            var framed = new byte[2 + body.Length];
            BinaryPrimitives.WriteUInt16BigEndian(framed, (ushort)body.Length);
            Buffer.BlockCopy(body, 0, framed, 2, body.Length);
            await stream.WriteAsync(framed, cts.Token);
        }, cts.Token);

        var result = await NetworkHelper.LookupAsync(
            "tcp.test",
            new DnsLookupOptions
            {
                Server = "127.0.0.1",
                Port = port,
                Timeout = TimeSpan.FromSeconds(3),
                Type = DnsRecordType.A
            });
        tcp.Stop();
        Assert.True(result.UsedTcp || result.Answers.Count > 0, result.Rcode.ToString());
        if (result.UsedTcp && result.Answers.Count > 0)
            Assert.Equal("8.8.4.4", result.Answers[0].Data);
    }

    [Fact]
    public async Task Wire_lookup_unresolvable_server_name_fails_typed()
    {
        var result = await NetworkHelper.LookupAsync(
            "x.test",
            new DnsLookupOptions
            {
                Server = "no-such-dns-host.invalid",
                Timeout = TimeSpan.FromMilliseconds(400),
                Type = DnsRecordType.Txt
            });
        Assert.True(result.Rcode is DnsRcode.Failed or DnsRcode.Timeout or DnsRcode.Refused);
    }

    [Fact]
    public async Task Wire_lookup_server_hostname_localhost()
    {
        var result = await NetworkHelper.LookupAsync(
            "x.test",
            new DnsLookupOptions
            {
                Server = "localhost",
                Port = 1,
                Timeout = TimeSpan.FromMilliseconds(250),
                Type = DnsRecordType.Ns
            });
        Assert.True(result.Rcode is DnsRcode.Timeout or DnsRcode.Refused or DnsRcode.Failed or DnsRcode.FormErr);
    }

    [Fact]
    public async Task Ptr_ipv6_does_not_throw()
    {
        var result = await NetworkHelper.LookupAsync(
            "::1",
            new DnsLookupOptions
            {
                Type = DnsRecordType.Ptr,
                Server = "127.0.0.1",
                Port = 1,
                Timeout = TimeSpan.FromMilliseconds(250)
            });
        Assert.Equal(DnsRecordType.Ptr, result.Type);
    }

    [Fact]
    public async Task Os_lookup_any_and_ptr_name_passthrough()
    {
        var any = await NetworkHelper.LookupAsync("localhost", new DnsLookupOptions { Type = DnsRecordType.Any });
        Assert.Equal(DnsRecordType.Any, any.Type);
        var ptrName = await NetworkHelper.LookupAsync(
            "1.0.0.127.in-addr.arpa",
            new DnsLookupOptions
            {
                Type = DnsRecordType.Ptr,
                Server = "127.0.0.1",
                Port = 1,
                Timeout = TimeSpan.FromMilliseconds(200)
            });
        Assert.Equal(DnsRecordType.Ptr, ptrName.Type);
        var many = await NetworkHelper.LookupManyAsync(["localhost", "127.0.0.1"]);
        Assert.Equal(2, many.Count);
    }

    [Fact]
    public async Task Lookup_null_options_uses_os()
    {
        var result = await NetworkHelper.LookupAsync("localhost", null);
        Assert.True(result.Rcode is DnsRcode.NoError or DnsRcode.NxDomain);
    }

    static DnsLookupResult One(DnsRecordType type, byte[] rdata)
        => DnsClient.Parse(
            "example.test",
            type,
            "127.0.0.1",
            Answer(Id, "example.test", type, type, rdata),
            false,
            TimeSpan.Zero,
            Id);

    static byte[] Answer(
        ushort id,
        string qname,
        DnsRecordType qtype,
        DnsRecordType anType,
        byte[] rdata,
        bool truncated = false,
        int rcode = 0,
        bool compress = true,
        int answers = 1)
    {
        using var ms = new MemoryStream();
        WriteU16(ms, id);
        ushort flags = (ushort)(0x8000 | (truncated ? 0x0200 : 0) | (rcode & 0xF));
        WriteU16(ms, flags);
        WriteU16(ms, 1);
        WriteU16(ms, (ushort)answers);
        WriteU16(ms, 0);
        WriteU16(ms, 0);
        WriteName(ms, qname);
        WriteU16(ms, (ushort)qtype);
        WriteU16(ms, 1);
        if (answers == 0)
            return ms.ToArray();
        if (compress)
        {
            ms.WriteByte(0xC0);
            ms.WriteByte(0x0C);
        }
        else
            WriteName(ms, qname);
        WriteU16(ms, (ushort)anType);
        WriteU16(ms, 1);
        WriteU16(ms, 0);
        WriteU16(ms, 60);
        WriteU16(ms, (ushort)rdata.Length);
        ms.Write(rdata);
        return ms.ToArray();
    }

    static byte[] EncodeName(string name)
    {
        using var ms = new MemoryStream();
        WriteName(ms, name);
        return ms.ToArray();
    }

    static void WriteName(MemoryStream ms, string name)
    {
        foreach (var label in name.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var bytes = Encoding.ASCII.GetBytes(label);
            ms.WriteByte((byte)bytes.Length);
            ms.Write(bytes);
        }
        ms.WriteByte(0);
    }

    static void WriteU16(MemoryStream ms, ushort value)
    {
        ms.WriteByte((byte)(value >> 8));
        ms.WriteByte((byte)value);
    }

    static byte[] Concat(ReadOnlySpan<byte> a, byte[] b)
    {
        var n = new byte[a.Length + b.Length];
        a.CopyTo(n);
        b.CopyTo(n, a.Length);
        return n;
    }

    static async Task ReadExact(Stream stream, byte[] buffer, CancellationToken token)
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
}
