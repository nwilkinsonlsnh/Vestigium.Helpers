using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Vestigium.Helpers.Watch.Dns;
using Xunit;

namespace Vestigium.Helpers.Watch.Dns.Tests;

public sealed class UnseenTests
{
    [Fact(Timeout = 3000)]
    public void Resolver_line_names_the_holes()
    {
        var row = Unseen.Line(packet: false);

        Assert.Equal(Unseen.Resolver, row.Answers);
        Assert.Equal("Unseen", row.Status);
        Assert.Equal("event", row.Mode);
    }

    [Fact(Timeout = 3000)]
    public void Packet_line_names_the_holes()
    {
        var row = Unseen.Line(WatchSource.Port);

        Assert.Equal(Unseen.Packet, row.Answers);
        Assert.Equal("port", row.Mode);
    }

    [Fact(Timeout = 3000)]
    public void Both_names_both_holes()
    {
        var row = Unseen.Line(WatchSource.Both);

        Assert.Contains("Raw sockets", row.Answers);
        Assert.Contains("this host sends", row.Answers);
        Assert.Contains("DoH, DoT, and DoQ", row.Answers);
        Assert.Equal("both", row.Mode);
    }

    [Fact(Timeout = 3000)]
    public void A_pipe_that_cannot_open_exits_4_and_writes_nothing()
    {
        var code = Unseen.OpenPipe("", out var pipe);

        Assert.Equal(Unseen.PipeFailed, code);
        Assert.Null(pipe);
    }

    [Fact(Timeout = 3000)]
    public async Task The_unseen_line_is_the_first_line()
    {
        var name = "watch-dns-" + Guid.NewGuid().ToString("N");
        var code = Unseen.OpenPipe(name, out var pipe);
        Assert.Equal(0, code);
        await using var open = pipe!;
        using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
        var connect = client.ConnectAsync(1_000);
        await open.WaitForClientAsync(new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token);
        await connect;

        await open.WriteAsync(Unseen.Line(packet: false), CancellationToken.None);
        using var reader = new StreamReader(client, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var reading = reader.ReadLineAsync();
        var line = await reading.WaitAsync(TimeSpan.FromSeconds(1));
        var row = JsonSerializer.Deserialize<WatchRow>(line!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal("Unseen", row!.Status);
        Assert.Equal(Unseen.Resolver, row.Answers);
    }
}
