using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class UdpProbeEngine
{
    public static NetworkJob<UdpProbeResult> Create(string host, int port, UdpProbeOptions? options)
    {
        var name = HelperGuard.NotBlank(host, nameof(host)).Trim();
        if (port is < 1 or > 65535)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Create), $"Port={port}");
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be 1–65535.");
        }

        var o = options ?? new UdpProbeOptions();
        var timeoutMs = o.Timeout.TotalMilliseconds;
        if (timeoutMs is < IcmpEchoOptions.MinTimeoutMs or > IcmpEchoOptions.MaxTimeoutMs)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Create), $"TimeoutMs={timeoutMs:0}");
            throw new ArgumentOutOfRangeException(nameof(o.Timeout), "Timeout must be between 10 ms and 60 s.");
        }

        if (o.PayloadSize is < 0 or > IcmpEchoOptions.MaxBufferSize)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Create), $"PayloadSize={o.PayloadSize}");
            throw new ArgumentOutOfRangeException(nameof(o.PayloadSize), "PayloadSize must be between 0 and 65500.");
        }

        EgressBind.Validate(o.InterfaceIndex, o.SourceAddress);
        TraceResolve.Guard(o.Family);
        TraceResolve.GuardLiteral(name, o.Family);
        var jobId = "udp-" + HelperLog.NewId();
        return new NetworkJob<UdpProbeResult>(jobId, "udpProbe", (token, progress) => RunAsync(jobId, name, port, o, token, progress));
    }

    private static async Task<UdpProbeResult> RunAsync(
        string jobId,
        string host,
        int port,
        UdpProbeOptions options,
        CancellationToken token,
        IProgress<NetworkProgress>? progress)
    {
        NetworkLog.Pending(HelperLog.Subcategories.Icmp, $"udpProbe job={jobId} host={host} port={port}");
        var timeoutMs = Math.Clamp((int)options.Timeout.TotalMilliseconds, IcmpEchoOptions.MinTimeoutMs, IcmpEchoOptions.MaxTimeoutMs);
        var dest = await ResolveAsync(host, options.Family, token).ConfigureAwait(false);
        if (dest is null)
        {
            var miss = new UdpProbeResult(jobId, host, port, null, UdpProbeStatus.TimedOut, 0, "unresolved");
            NetworkLog.Failed(HelperLog.Subcategories.Icmp, $"TimedOut udpProbe job={jobId} host={host} unresolved");
            return miss;
        }

        var started = Stopwatch.StartNew();
        try
        {
            using var socket = new Socket(dest.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            EgressBind.Apply(socket, options.InterfaceIndex, options.SourceAddress);
            var payload = new byte[options.PayloadSize];
            await socket.SendToAsync(payload, SocketFlags.None, new IPEndPoint(dest, port), token).ConfigureAwait(false);
            var buffer = new byte[Math.Max(512, options.PayloadSize + 64)];
            using var timed = CancellationTokenSource.CreateLinkedTokenSource(token);
            timed.CancelAfter(timeoutMs);
            try
            {
                var got = await socket.ReceiveFromAsync(
                    buffer,
                    SocketFlags.None,
                    new IPEndPoint(dest.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0),
                    timed.Token).ConfigureAwait(false);
                var remote = got.RemoteEndPoint as IPEndPoint;
                var ok = new UdpProbeResult(jobId, host, port, (remote?.Address ?? dest).ToString(), UdpProbeStatus.Replied, started.ElapsedMilliseconds, "replied");
                progress?.Report(new NetworkProgress { JobId = jobId, Phase = "Udp", LastStatus = "Replied", LastRoundtripMs = ok.ElapsedMs });
                NetworkLog.Success(HelperLog.Subcategories.Icmp, $"Replied udpProbe job={jobId} host={host} port={port} ms={ok.ElapsedMs}");
                return ok;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                var to = new UdpProbeResult(jobId, host, port, dest.ToString(), UdpProbeStatus.TimedOut, started.ElapsedMilliseconds, "timeout");
                NetworkLog.Failed(HelperLog.Subcategories.Icmp, $"TimedOut udpProbe job={jobId} host={host} port={port} ms={to.ElapsedMs}");
                return to;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SocketException ex) when (ex.SocketErrorCode is SocketError.ConnectionReset or SocketError.HostUnreachable or SocketError.NetworkUnreachable or SocketError.MessageSize)
        {
            var unreach = new UdpProbeResult(jobId, host, port, dest.ToString(), UdpProbeStatus.Unreachable, started.ElapsedMilliseconds, ex.SocketErrorCode.ToString());
            NetworkLog.Warning(HelperLog.Subcategories.Icmp, $"Unreachable udpProbe job={jobId} host={host} port={port}");
            return unreach;
        }
        catch (SocketException ex)
        {
            var to = new UdpProbeResult(jobId, host, port, dest.ToString(), UdpProbeStatus.TimedOut, started.ElapsedMilliseconds, ex.SocketErrorCode.ToString());
            NetworkLog.Failed(HelperLog.Subcategories.Icmp, $"TimedOut udpProbe job={jobId} host={host} port={port} {ex.SocketErrorCode}");
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
