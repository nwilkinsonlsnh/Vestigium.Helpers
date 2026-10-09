using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Vestigium.Helpers.Watch.Dns;
using Xunit;

namespace Vestigium.Helpers.Watch.Dns.Tests;

public sealed class WatchRunTests
{
    [Fact(Timeout = 3000)]
    public async Task Both_keeps_the_port_when_the_event_session_throws()
    {
        var name = "watch-dns-" + Guid.NewGuid().ToString("N");
        var opened = Unseen.OpenPipe(name, out var pipe);
        Assert.Equal(0, opened);
        await using var open = pipe!;
        using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
        var connect = client.ConnectAsync(1_000);
        await open.WaitForClientAsync(new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token);
        await connect;
        Assert.True(WatchRequest.TryCreate(WatchSource.Both, 5, out var request, out _));

        var code = await WatchRun.RunAsync(
            open,
            request!,
            (_, _, _) => throw new InvalidOperationException("session refused"),
            (_, _, _) => Task.FromResult(0),
            CancellationToken.None);

        using var reader = new StreamReader(client, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var reading = reader.ReadLineAsync();
        var line = await reading.WaitAsync(TimeSpan.FromSeconds(1));
        var row = JsonSerializer.Deserialize<WatchRow>(line!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(0, code);
        Assert.Equal("Failed", row!.Status);
        Assert.Equal("session refused", row.Answers);
        request.Clock.Stop();
    }

    [Fact(Timeout = 3000)]
    public async Task Event_only_exits_3_when_the_session_throws()
    {
        var name = "watch-dns-" + Guid.NewGuid().ToString("N");
        var opened = Unseen.OpenPipe(name, out var pipe);
        Assert.Equal(0, opened);
        await using var open = pipe!;
        using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
        var connect = client.ConnectAsync(1_000);
        await open.WaitForClientAsync(new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token);
        await connect;
        Assert.True(WatchRequest.TryCreate(WatchSource.Event, 5, out var request, out _));

        var code = await WatchRun.RunAsync(
            open,
            request!,
            (_, _, _) => throw new InvalidOperationException("session refused"),
            (_, _, _) => Task.FromResult(0),
            CancellationToken.None);

        Assert.Equal(ResolverWatch.SessionFailed, code);
        request.Clock.Stop();
    }
}
