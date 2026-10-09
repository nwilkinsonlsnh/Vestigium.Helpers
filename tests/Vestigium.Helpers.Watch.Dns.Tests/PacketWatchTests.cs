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
    }

    [Fact]
    public void A_response_is_not_a_question()
    {
        var payload = Question("edge.example", 1);
        payload[2] = 0x80;

        var row = PacketWatch.Read(payload);

        Assert.Equal("", row.Name);
        Assert.Equal("Not a question", row.Status);
        Assert.Equal("packet", row.Mode);
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
}
