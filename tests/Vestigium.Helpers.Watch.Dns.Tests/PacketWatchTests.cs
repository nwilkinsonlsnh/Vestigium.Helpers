using Vestigium.Helpers.Watch.Dns;
using Xunit;

namespace Vestigium.Helpers.Watch.Dns.Tests;

public sealed class PacketWatchTests
{
    [Fact]
    public void Packet_mode_is_off_unless_asked()
    {
        Assert.False(PacketWatch.Requested([]));
        Assert.False(PacketWatch.Requested(["resolver"]));
        Assert.True(PacketWatch.Requested(["packet"]));
    }

    [Fact]
    public void A_question_keeps_the_name()
    {
        var row = PacketWatch.Read(Question("edge.example", 1));

        Assert.Equal("edge.example", row.Name);
        Assert.Equal("1", row.Type);
        Assert.Equal("packet", row.Mode);
        Assert.Equal("", row.Status);
        Assert.Equal(1, row.ResolverCount);
        Assert.Equal(0, row.PacketCount);
    }

    [Fact]
    public void A_response_carries_status_and_answers()
    {
        var row = PacketWatch.Read(Response("edge.example", 1, "1.2.3.4"));

        Assert.Equal("edge.example", row.Name);
        Assert.Equal("1", row.Type);
        Assert.Equal("NOERROR", row.Status);
        Assert.Equal("1.2.3.4", row.Answers);
        Assert.Equal(0, row.ResolverCount);
        Assert.Equal(1, row.PacketCount);
    }

    [Fact]
    public void An_outbound_udp_53_frame_keeps_the_question()
    {
        var question = Question("edge.example", 1);
        var frame = new byte[20 + 8 + question.Length];
        frame[0] = 0x45;
        frame[9] = 17;
        frame[20] = 0;
        frame[21] = 53;
        frame[24] = 0;
        frame[25] = (byte)(8 + question.Length);
        question.CopyTo(frame, 28);

        Assert.True(OutboundFrame.TryRead(frame, out var payload));
        var row = PacketWatch.Read(payload);
        Assert.Equal("edge.example", row.Name);
    }

    private static byte[] Question(string name, int type)
    {
        var labels = name.Split('.');
        var body = new List<byte> { 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 };
        foreach (var label in labels)
        {
            body.Add((byte)label.Length);
            body.AddRange(System.Text.Encoding.ASCII.GetBytes(label));
        }
        body.Add(0);
        body.Add((byte)(type >> 8));
        body.Add((byte)type);
        body.Add(0);
        body.Add(1);
        return body.ToArray();
    }

    private static byte[] Response(string name, int type, string address)
    {
        var question = Question(name, type);
        var answer = new List<byte>();
        answer.Add(0xC0);
        answer.Add(12);
        answer.Add((byte)(type >> 8));
        answer.Add((byte)type);
        answer.Add(0);
        answer.Add(1);
        answer.Add(0);
        answer.Add(0);
        answer.Add(0);
        answer.Add(60);
        var octets = address.Split('.').Select(byte.Parse).ToArray();
        answer.Add(0);
        answer.Add((byte)octets.Length);
        answer.AddRange(octets);

        var body = question.ToList();
        body[2] = 0x80;
        body[6] = 0;
        body[7] = 1;
        body.AddRange(answer);
        return body.ToArray();
    }
}
