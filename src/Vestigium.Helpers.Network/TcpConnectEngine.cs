using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class TcpConnectEngine
{
    public static NetworkJob<TcpConnectResult> Create(string host, int port, TcpConnectOptions? options)
    {
        var name = HelperGuard.NotBlank(host, nameof(host)).Trim();
        if (port is < 1 or > 65535)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Create), $"Port={port}");
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be 1–65535.");
        }

        var o = options ?? new TcpConnectOptions();
        var timeoutMs = o.Timeout.TotalMilliseconds;
        if (timeoutMs is < IcmpEchoOptions.MinTimeoutMs or > IcmpEchoOptions.MaxTimeoutMs)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Create), $"TimeoutMs={timeoutMs:0}");
            throw new ArgumentOutOfRangeException(nameof(o.Timeout), "Timeout must be between 10 ms and 60 s.");
        }

        EgressBind.Validate(o.InterfaceIndex, o.SourceAddress);
        TraceResolve.Guard(o.Family);
        TraceResolve.GuardLiteral(name, o.Family);
        var jobId = "tcp-" + HelperLog.NewId();
        return new NetworkJob<TcpConnectResult>(jobId, "tcpConnect", (token, progress) => RunAsync(jobId, name, port, o, token, progress));
    }

    private static async Task<TcpConnectResult> RunAsync(
        string jobId,
        string host,
        int port,
        TcpConnectOptions options,
        CancellationToken token,
        IProgress<NetworkProgress>? progress)
    {
        NetworkLog.Pending(HelperLog.Subcategories.Icmp, $"tcpConnect job={jobId} host={host} port={port}");
        var timeoutMs = Math.Clamp((int)options.Timeout.TotalMilliseconds, IcmpEchoOptions.MinTimeoutMs, IcmpEchoOptions.MaxTimeoutMs);
        var dest = await ResolveAsync(host, options.Family, token).ConfigureAwait(false);
        if (dest is null)
        {
            var miss = new TcpConnectResult(jobId, host, port, null, TcpConnectStatus.TimedOut, 0, "unresolved");
            NetworkLog.Failed(HelperLog.Subcategories.Icmp, $"TimedOut tcpConnect job={jobId} host={host} port={port} unresolved");
            return miss;
        }

        var started = Stopwatch.StartNew();
        try
        {
            using var socket = new Socket(dest.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            EgressBind.Apply(socket, options.InterfaceIndex, options.SourceAddress);
            socket.NoDelay = true;
            using var timed = CancellationTokenSource.CreateLinkedTokenSource(token);
            timed.CancelAfter(timeoutMs);
            try
            {
                await socket.ConnectAsync(dest, port, timed.Token).ConfigureAwait(false);
                var ok = new TcpConnectResult(jobId, host, port, dest.ToString(), TcpConnectStatus.Connected, started.ElapsedMilliseconds, "connected");
                progress?.Report(new NetworkProgress { JobId = jobId, Phase = "Connect", LastStatus = "Connected", LastRoundtripMs = ok.ElapsedMs });
                NetworkLog.Success(HelperLog.Subcategories.Icmp, $"Connected tcpConnect job={jobId} host={host} port={port} ms={ok.ElapsedMs}");
                return ok;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                var to = new TcpConnectResult(jobId, host, port, dest.ToString(), TcpConnectStatus.TimedOut, started.ElapsedMilliseconds, "timeout");
                NetworkLog.Failed(HelperLog.Subcategories.Icmp, $"TimedOut tcpConnect job={jobId} host={host} port={port} ms={to.ElapsedMs}");
                return to;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SocketException ex) when (ex.SocketErrorCode is SocketError.ConnectionRefused or SocketError.ConnectionReset)
        {
            var refused = new TcpConnectResult(jobId, host, port, dest.ToString(), TcpConnectStatus.Refused, started.ElapsedMilliseconds, ex.SocketErrorCode.ToString());
            NetworkLog.Warning(HelperLog.Subcategories.Icmp, $"Refused tcpConnect job={jobId} host={host} port={port} ms={refused.ElapsedMs}");
            return refused;
        }
        catch (SocketException ex)
        {
            var to = new TcpConnectResult(jobId, host, port, dest.ToString(), TcpConnectStatus.TimedOut, started.ElapsedMilliseconds, ex.SocketErrorCode.ToString());
            NetworkLog.Failed(HelperLog.Subcategories.Icmp, $"TimedOut tcpConnect job={jobId} host={host} port={port} {ex.SocketErrorCode}");
            return to;
        }
    }

    private static async Task<IPAddress?> ResolveAsync(string host, RouteFamily family, CancellationToken token)
    {
        if (IPAddress.TryParse(host, out var parsed))
        {
            if (TraceResolve.Pin(family) is { } required && parsed.AddressFamily != required)
                return null;
            return parsed;
        }

        try
        {
            var text = await TraceResolve.ResolveAsync(host, family, token).ConfigureAwait(false);
            if (IPAddress.TryParse(text, out var fromPin))
                return fromPin;
            var pin = TraceResolve.Pin(family);
            var addrs = pin is { } required
                ? await Dns.GetHostAddressesAsync(host, required, token).ConfigureAwait(false)
                : await Dns.GetHostAddressesAsync(host, token).ConfigureAwait(false);
            return TraceResolve.Pick(addrs, family);
        }
        catch (SocketException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
