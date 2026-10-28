using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class SnmpSysDescrEngine
{
    public static NetworkJob<SnmpSysDescrResult> Create(string host, SnmpSysDescrOptions? options)
    {
        var name = HelperGuard.NotBlank(host, nameof(host)).Trim();
        var o = options ?? new SnmpSysDescrOptions();
        if (string.IsNullOrWhiteSpace(o.Community))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Create), "Community missing");
            throw new ArgumentException("Community is required.", nameof(options));
        }

        if (o.Port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(o.Port), "Port must be 1–65535.");
        var timeoutMs = o.Timeout.TotalMilliseconds;
        if (timeoutMs is < IcmpEchoOptions.MinTimeoutMs or > IcmpEchoOptions.MaxTimeoutMs)
            throw new ArgumentOutOfRangeException(nameof(o.Timeout), "Timeout must be between 10 ms and 60 s.");

        EgressBind.Validate(o.InterfaceIndex, o.SourceAddress);
        TraceResolve.Guard(o.Family);
        TraceResolve.GuardLiteral(name, o.Family);
        var jobId = "snmp-" + HelperLog.NewId();
        return new NetworkJob<SnmpSysDescrResult>(jobId, "snmpSysDescr", (token, progress) => RunAsync(jobId, name, o, token, progress));
    }

    private static async Task<SnmpSysDescrResult> RunAsync(
        string jobId,
        string host,
        SnmpSysDescrOptions options,
        CancellationToken token,
        IProgress<NetworkProgress>? progress)
    {
        NetworkLog.Pending(HelperLog.Subcategories.Icmp, $"snmpSysDescr job={jobId} host={host} port={options.Port}");
        var timeoutMs = Math.Clamp((int)options.Timeout.TotalMilliseconds, IcmpEchoOptions.MinTimeoutMs, IcmpEchoOptions.MaxTimeoutMs);
        var dest = await ResolveAsync(host, options.Family, token).ConfigureAwait(false);
        if (dest is null)
            return Miss(jobId, host, null, 0, SnmpSysDescrStatus.TimedOut, "unresolved");

        var started = Stopwatch.StartNew();
        try
        {
            using var socket = new Socket(dest.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            EgressBind.Apply(socket, options.InterfaceIndex, options.SourceAddress);
            var request = SnmpSysDescrPacket.GetRequest(options.Community, Random.Shared.Next(1, int.MaxValue));
            await socket.SendToAsync(request, SocketFlags.None, new IPEndPoint(dest, options.Port), token).ConfigureAwait(false);
            var buffer = new byte[1500];
            using var timed = CancellationTokenSource.CreateLinkedTokenSource(token);
            timed.CancelAfter(timeoutMs);
            try
            {
                var got = await socket.ReceiveFromAsync(
                    buffer,
                    SocketFlags.None,
                    new IPEndPoint(dest.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0),
                    timed.Token).ConfigureAwait(false);
                var text = SnmpSysDescrPacket.ParseSysDescr(buffer.AsSpan(0, got.ReceivedBytes));
                if (string.IsNullOrWhiteSpace(text))
                    return Miss(jobId, host, dest.ToString(), started.ElapsedMilliseconds, SnmpSysDescrStatus.Error, "empty");
                var ok = new SnmpSysDescrResult(jobId, host, dest.ToString(), SnmpSysDescrStatus.Answered, text, started.ElapsedMilliseconds, "answered");
                progress?.Report(new NetworkProgress { JobId = jobId, Phase = "Snmp", LastStatus = "Answered", LastRoundtripMs = ok.ElapsedMs });
                NetworkLog.Success(HelperLog.Subcategories.Icmp, $"Answered snmpSysDescr job={jobId} host={host} ms={ok.ElapsedMs}");
                return ok;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                return Miss(jobId, host, dest.ToString(), started.ElapsedMilliseconds, SnmpSysDescrStatus.TimedOut, "timeout");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SocketException)
        {
            return Miss(jobId, host, dest.ToString(), started.ElapsedMilliseconds, SnmpSysDescrStatus.Error, "socket");
        }
    }

    private static SnmpSysDescrResult Miss(string jobId, string host, string? address, long elapsed, SnmpSysDescrStatus status, string detail)
    {
        NetworkLog.Failed(HelperLog.Subcategories.Icmp, $"{status} snmpSysDescr job={jobId} host={host} {detail}");
        return new SnmpSysDescrResult(jobId, host, address, status, null, elapsed, detail);
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
