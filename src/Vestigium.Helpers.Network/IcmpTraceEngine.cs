using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class IcmpTraceEngine
{
    public static NetworkJob<IcmpTraceResult> Create(string target, IcmpTraceOptions? options)
    {
        var host = HelperGuard.NotBlank(target, nameof(target)).Trim();
        var o = options ?? new IcmpTraceOptions();
        Guard(o);
        var jobId = "trace-" + HelperLog.NewId();
        return new NetworkJob<IcmpTraceResult>(jobId, "icmpTrace", (token, progress) => RunAsync(jobId, host, o, token, progress));
    }

    static void Guard(IcmpTraceOptions o)
    {
        if (o.MaxHops is < 1 or > 64)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"MaxHops={o.MaxHops}");
            throw new ArgumentOutOfRangeException(nameof(o.MaxHops), "MaxHops must be between 1 and 64.");
        }

        if (o.ProbesPerHop is < 1 or > 10)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"ProbesPerHop={o.ProbesPerHop}");
            throw new ArgumentOutOfRangeException(nameof(o.ProbesPerHop), "ProbesPerHop must be between 1 and 10.");
        }

        var timeoutMs = o.Timeout.TotalMilliseconds;
        if (timeoutMs < IcmpEchoOptions.MinTimeoutMs || timeoutMs > IcmpEchoOptions.MaxTimeoutMs)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"TimeoutMs={timeoutMs:0}");
            throw new ArgumentOutOfRangeException(nameof(o.Timeout), "Timeout must be between 10 ms and 60 s.");
        }
    }

    static async Task<IcmpTraceResult> RunAsync(
        string jobId,
        string target,
        IcmpTraceOptions options,
        CancellationToken token,
        IProgress<NetworkProgress>? progress)
    {
        NetworkLog.Pending(
            HelperLog.Subcategories.Icmp,
            $"trace job={jobId} target={target} maxHops={options.MaxHops} probes={options.ProbesPerHop}");

        var hops = new List<IcmpTraceHop>();
        var protocol = options.PreferUdp ? ProbeProtocol.Udp : ProbeProtocol.Icmp;
        var reached = false;
        string? resolved = IPAddress.TryParse(target, out var parsed) ? parsed.ToString() : null;
        var timeoutMs = Math.Clamp((int)options.Timeout.TotalMilliseconds, IcmpEchoOptions.MinTimeoutMs, IcmpEchoOptions.MaxTimeoutMs);
        var buffer = new byte[Math.Clamp(options.BufferSize, 0, IcmpEchoOptions.MaxBufferSize)];

        try
        {
            using var ping = new Ping();
            for (var ttl = 1; ttl <= options.MaxHops && !token.IsCancellationRequested && !reached; ttl++)
            {
                var probes = new List<IcmpTraceProbe>(options.ProbesPerHop);
                string? hopAddress = null;
                for (var probe = 1; probe <= options.ProbesPerHop && !token.IsCancellationRequested; probe++)
                {
                    IcmpTraceProbe row;
                    if (protocol == ProbeProtocol.Icmp && !options.PreferUdp)
                    {
                        row = await IcmpProbeAsync(ping, target, buffer, timeoutMs, ttl, probe, token).ConfigureAwait(false);
                        if (row.Status == IcmpEchoStatus.ProtocolForbidden)
                        {
                            protocol = ProbeProtocol.Udp;
                            NetworkLog.Warning(HelperLog.Subcategories.Icmp, $"trace job={jobId} ICMP forbidden; UDP fallback");
                            row = await UdpProbeAsync(target, timeoutMs, ttl, probe, token).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        row = await UdpProbeAsync(target, timeoutMs, ttl, probe, token).ConfigureAwait(false);
                    }

                    probes.Add(row);
                    hopAddress ??= row.Address;
                    if (row.Status == IcmpEchoStatus.Success)
                        reached = true;
                }

                hops.Add(new IcmpTraceHop(ttl, hopAddress, probes));
                progress?.Report(new NetworkProgress
                {
                    JobId = jobId,
                    Phase = "Trace",
                    Sent = hops.Count,
                    Received = hops.Count(h => h.Address is not null),
                    Sequence = ttl,
                    LastStatus = reached ? "Reached" : hopAddress ?? "*"
                });
            }
        }
        catch (OperationCanceledException)
        {
        }

        var status = token.IsCancellationRequested
            ? NetworkJobStatus.Cancelled
            : reached
                ? NetworkJobStatus.Success
                : hops.SelectMany(h => h.Probes).Any(p => p.Status == IcmpEchoStatus.ProtocolForbidden) && hops.All(h => h.Address is null)
                    ? NetworkJobStatus.Failed
                    : NetworkJobStatus.TimedOut;

        NetworkLog.Success(
            HelperLog.Subcategories.Icmp,
            $"{status} trace job={jobId} target={target} hops={hops.Count} reached={reached} protocol={protocol}");

        return new IcmpTraceResult(jobId, target, resolved, status, reached, protocol, hops.Count, hops);
    }

    internal static async Task<IcmpTraceProbe> IcmpProbeAsync(
        Ping ping,
        string target,
        byte[] buffer,
        int timeoutMs,
        int ttl,
        int probe,
        CancellationToken token)
    {
        try
        {
            PingReply reply;
            var options = new PingOptions(ttl, true);
            try
            {
                reply = await ping.SendPingAsync(target, TimeSpan.FromMilliseconds(timeoutMs), buffer, options, token)
                    .ConfigureAwait(false);
            }
            catch (PlatformNotSupportedException) when (buffer.Length > 0)
            {
                reply = await ping.SendPingAsync(target, TimeSpan.FromMilliseconds(timeoutMs), [], options, token)
                    .ConfigureAwait(false);
            }

            var status = MapStatus(reply.Status);
            var address = MapAddress(reply.Address);
            return new IcmpTraceProbe(ttl, probe, ProbeProtocol.Icmp, status, address, reply.RoundtripTime, reply.Status.ToString());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (PlatformNotSupportedException)
        {
            return new IcmpTraceProbe(ttl, probe, ProbeProtocol.Icmp, IcmpEchoStatus.ProtocolForbidden, null, 0, "ICMP not permitted");
        }
        catch (PingException ex)
        {
            var forbidden = ex.Message.Contains("not permitted", StringComparison.OrdinalIgnoreCase)
                || ex.InnerException is PlatformNotSupportedException;
            return new IcmpTraceProbe(
                ttl,
                probe,
                ProbeProtocol.Icmp,
                forbidden ? IcmpEchoStatus.ProtocolForbidden : IcmpEchoStatus.Failed,
                null,
                0,
                ex.InnerException?.Message ?? ex.Message);
        }
    }

    internal static async Task<IcmpTraceProbe> UdpProbeAsync(
        string target,
        int timeoutMs,
        int ttl,
        int probe,
        CancellationToken token)
    {
        if (!IPAddress.TryParse(target, out var dest))
        {
            try
            {
                var addrs = await Dns.GetHostAddressesAsync(target, token).ConfigureAwait(false);
                dest = addrs.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                    ?? addrs.FirstOrDefault();
            }
            catch (SocketException)
            {
                dest = null;
            }
        }

        if (dest is null)
            return new IcmpTraceProbe(ttl, probe, ProbeProtocol.Udp, IcmpEchoStatus.Failed, null, 0, "unresolved");

        try
        {
            using var socket = new Socket(dest.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.Ttl = (short)ttl;
            socket.ReceiveTimeout = timeoutMs;
            var remote = new IPEndPoint(dest, 33434 + ttl);
            var started = System.Diagnostics.Stopwatch.StartNew();
            await socket.SendToAsync(new byte[32], SocketFlags.None, remote, token).ConfigureAwait(false);
            var buffer = new byte[512];
            try
            {
                var received = await socket.ReceiveFromAsync(buffer, new IPEndPoint(
                    dest.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0))
                    .WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), token)
                    .ConfigureAwait(false);
                var from = received.RemoteEndPoint as IPEndPoint;
                var address = from?.Address.ToString();
                var reached = from is not null && from.Address.Equals(dest);
                return new IcmpTraceProbe(
                    ttl,
                    probe,
                    ProbeProtocol.Udp,
                    reached ? IcmpEchoStatus.Success : IcmpEchoStatus.TtlExpired,
                    address,
                    started.ElapsedMilliseconds,
                    reached ? "udp-reached" : "udp-hop");
            }
            catch (TimeoutException)
            {
                return new IcmpTraceProbe(ttl, probe, ProbeProtocol.Udp, IcmpEchoStatus.TimedOut, null, timeoutMs, "timeout");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SocketException ex)
        {
            return new IcmpTraceProbe(ttl, probe, ProbeProtocol.Udp, IcmpEchoStatus.TimedOut, null, 0, ex.SocketErrorCode.ToString());
        }
    }

    internal static IcmpEchoStatus MapStatus(IPStatus status) => status switch
    {
        IPStatus.Success => IcmpEchoStatus.Success,
        IPStatus.TimedOut => IcmpEchoStatus.TimedOut,
        IPStatus.TimeExceeded or IPStatus.TtlExpired or IPStatus.TtlReassemblyTimeExceeded => IcmpEchoStatus.TtlExpired,
        IPStatus.DestinationNetworkUnreachable
            or IPStatus.DestinationHostUnreachable
            or IPStatus.DestinationUnreachable => IcmpEchoStatus.DestinationUnreachable,
        _ => IcmpEchoStatus.Failed
    };

    internal static string? MapAddress(IPAddress? address)
        => address is null || Equals(address, IPAddress.Any) || Equals(address, IPAddress.IPv6Any)
            ? null
            : address.ToString();
}
