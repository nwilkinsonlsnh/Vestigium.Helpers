using Microsoft.Diagnostics.Tracing.Session;

namespace Vestigium.Helpers.Watch.Dns;

internal static class DnsClientSession
{
    public static async Task RunAsync(WatchPipe pipe, WatchClock clock, CancellationToken token)
    {
        using var session = new TraceEventSession("Vestigium-Watch-Dns-" + Guid.NewGuid().ToString("N"));
        session.EnableProvider(ResolverWatch.ProviderId);
        session.Source.Dynamic.All += data =>
        {
            if (data.ID != ResolverWatch.QueryCompleted)
                return;

            var row = ResolverWatch.Map(
                data.ID,
                data.ProcessID,
                Payload(data, "QueryName"),
                Payload(data, "QueryType"),
                Payload(data, "QueryStatus"),
                Payload(data, "QueryResults"));
            if (row is null)
                return;

            pipe.WriteAsync(row, CancellationToken.None).GetAwaiter().GetResult();
        };

        using var stop = token.Register(() => session.Stop());
        var processing = Task.Run(() => session.Source.Process(), token);
        await Task.WhenAny(processing, clock.Completion).ConfigureAwait(false);
        session.Stop();
        await processing.ConfigureAwait(false);
    }

    private static string? Payload(Microsoft.Diagnostics.Tracing.TraceEvent data, string name)
    {
        try
        {
            return data.PayloadByName(name)?.ToString();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
