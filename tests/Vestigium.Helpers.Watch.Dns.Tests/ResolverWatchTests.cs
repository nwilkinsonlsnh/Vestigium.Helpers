using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Vestigium.Helpers.Watch.Dns;
using Xunit;

namespace Vestigium.Helpers.Watch.Dns.Tests;

public sealed class ResolverWatchTests
{
    [Fact(Timeout = 3000)]
    public void Other_event_ids_are_dropped()
    {
        Assert.Null(ResolverWatch.Map(3006, 10, "edge.example", "1", "0", "1.2.3.4"));
    }

    [Fact(Timeout = 3000)]
    public void Event_3008_keeps_the_query_and_the_caller_pid()
    {
        var row = ResolverWatch.Map(3008, 44, " edge.example ", "1", "0", "1.2.3.4");

        Assert.NotNull(row);
        Assert.Equal(44, row.Pid);
        Assert.Equal("edge.example", row.Name);
        Assert.Equal("1", row.Type);
        Assert.Equal("0", row.Status);
        Assert.Equal("1.2.3.4", row.Answers);
        Assert.Equal("resolver", row.Mode);
    }

    [Fact(Timeout = 3000)]
    public async Task A_session_that_cannot_start_writes_one_failure_and_returns_3()
    {
        var name = "watch-dns-" + Guid.NewGuid().ToString("N");
        await using var pipe = WatchPipe.Create(name);
        using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
        var connect = client.ConnectAsync(5_000);
        await pipe.WaitForClientAsync(new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);
        await connect;
        Assert.True(WatchClock.TryCreate(5, out var clock, out _));

        var code = await ResolverWatch.RunAsync(
            pipe,
            clock!,
            (_, _, _) => throw new InvalidOperationException("session refused"),
            CancellationToken.None);

        using var reader = new StreamReader(client, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var line = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(2));
        var row = JsonSerializer.Deserialize<WatchRow>(line!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(ResolverWatch.SessionFailed, code);
        Assert.Equal("Failed", row!.Status);
        Assert.Equal("session refused", row.Answers);
        Assert.Equal("resolver", row.Mode);
        clock.Stop();
    }
}
