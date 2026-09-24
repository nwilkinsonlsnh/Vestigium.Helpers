using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

// Linux ICMP path (PR02.005): System.Net.NetworkInformation.Ping.
// On current .NET 10 this is unprivileged ICMP DGRAM when the kernel allows it.
// Never spawn ping(8). Never open a raw socket here.
// Custom payload rejected → empty-buffer retry + PayloadRestricted.
// Access denied / not permitted → ProtocolForbidden.
// A dedicated DGRAM socket is PR04 only if BCL Ping is proven raw-only.
internal static class IcmpEchoEngine
{
    public static NetworkJob<IcmpEchoResult> Create(string target, IcmpEchoOptions? options)
    {
        var host = HelperGuard.NotBlank(target, nameof(target)).Trim();
        var o = options ?? new IcmpEchoOptions();
        Guard(o);
        var jobId = "icmp-" + HelperLog.NewId();
        return new NetworkJob<IcmpEchoResult>(jobId, "icmpEcho", async (token, progress) =>
        {
            var result = await RunAsync(jobId, host, o, token, progress).ConfigureAwait(false);
            IcmpEchoStats.WriteIfRequested(o.StatsPath, result);
            return result;
        });
    }

    private static void Guard(IcmpEchoOptions o)
    {
        EgressBind.Validate(o.InterfaceIndex, o.SourceAddress);
        if (o.Count < 0)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"Count={o.Count} is below 0");
            throw new ArgumentOutOfRangeException(nameof(o.Count), "Count must be 0 (continuous) or at least 1.");
        }

        var timeoutMs = o.Timeout.TotalMilliseconds;
        if (timeoutMs is < IcmpEchoOptions.MinTimeoutMs or > IcmpEchoOptions.MaxTimeoutMs)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"TimeoutMs={timeoutMs:0}");
            throw new ArgumentOutOfRangeException(nameof(o.Timeout), "Timeout must be between 10 ms and 60 s.");
        }

        if (o.Interval < TimeSpan.Zero)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), "Interval is negative");
            throw new ArgumentOutOfRangeException(nameof(o.Interval), "Interval cannot be negative.");
        }

        if (o.BufferSize is < IcmpEchoOptions.MinBufferSize or > IcmpEchoOptions.MaxBufferSize)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"BufferSize={o.BufferSize}");
            throw new ArgumentOutOfRangeException(nameof(o.BufferSize), "BufferSize must be between 1 and 65500.");
        }

        if (o.Ttl is < 1 or > 255)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"Ttl={o.Ttl}");
            throw new ArgumentOutOfRangeException(nameof(o.Ttl), $"Ttl={o.Ttl}");
        }

        if (o.MaxDuration is { } explicitDuration
            && (explicitDuration <= TimeSpan.Zero || explicitDuration > IcmpEchoOptions.MaxJobDuration))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"MaxDuration={explicitDuration}");
            throw new ArgumentOutOfRangeException(nameof(o.MaxDuration), "MaxDuration must be greater than 0 and at most 24 hours.");
        }

        if (o.Count != 0)
            return;

        o.MaxDuration ??= IcmpEchoOptions.DefaultContinuousDuration;
        var duration = o.MaxDuration.Value;

        if (duration > IcmpEchoOptions.ShortContinuousLimit)
        {
            if (o.AllowBurst)
            {
                HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), "burst past one minute");
                throw new ArgumentException("AllowBurst is limited to continuous jobs of one minute or less.", nameof(o.AllowBurst));
            }

            if (o.Interval >= IcmpEchoOptions.MinLongContinuousInterval) return;
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), "long interval below 1s");
            throw new ArgumentOutOfRangeException(
                nameof(o.Interval),
                "Continuous jobs longer than one minute require an Interval of at least 1 second.");
        }

        if (o.AllowBurst || o.Interval >= IcmpEchoOptions.MinContinuousInterval) return;
        HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), "interval below 200ms");
        throw new ArgumentOutOfRangeException(
            nameof(o.Interval),
            "Continuous jobs require an Interval of at least 200 ms unless AllowBurst is set.");
    }

    private static async Task<IcmpEchoResult> RunAsync(
        string jobId,
        string target,
        IcmpEchoOptions options,
        CancellationToken token,
        IProgress<NetworkProgress>? progress)
    {
        NetworkLog.Pending(
            HelperLog.Subcategories.Icmp,
            $"recipe job={jobId} target={target} count={options.Count} timeoutMs={(int)options.Timeout.TotalMilliseconds} buffer={options.BufferSize} intervalMs={(int)options.Interval.TotalMilliseconds}");

        var replies = new List<IcmpEchoReply>();
        var payloadRestricted = false;
        var warnedPayload = false;
        string? resolved = null;
        var started = Stopwatch.StartNew();
        var buffer = new byte[options.BufferSize];
        var pingOptions = new PingOptions(options.Ttl, options.DontFragment);
        var timeoutMs = Math.Clamp((int)options.Timeout.TotalMilliseconds, IcmpEchoOptions.MinTimeoutMs, IcmpEchoOptions.MaxTimeoutMs);

        try
        {
            using var ping = new Ping();
            var sequence = 0;
            while (!token.IsCancellationRequested)
            {
                if (options.MaxDuration is { } cap && started.Elapsed >= cap)
                    break;
                if (options.Count > 0 && sequence >= options.Count)
                    break;

                sequence++;
                var reply = await SendOnceAsync(ping, target, buffer, timeoutMs, pingOptions, sequence, token).ConfigureAwait(false);
                if (reply.PayloadRestricted)
                {
                    payloadRestricted = true;
                    buffer = [];
                    if (!warnedPayload)
                    {
                        NetworkLog.Warning(HelperLog.Subcategories.Icmp, $"payload restricted job={jobId}; retrying empty buffer");
                        warnedPayload = true;
                    }
                }

                resolved ??= reply.Address;
                replies.Add(reply);
                progress?.Report(new NetworkProgress
                {
                    JobId = jobId,
                    Phase = "Echo",
                    Sent = replies.Count,
                    Received = replies.Count(r => r.Status == IcmpEchoStatus.Success),
                    Sequence = sequence,
                    LastStatus = reply.Status.ToString(),
                    LastRoundtripMs = reply.RoundtripTimeMs
                });

                if (options.Count > 0 && sequence >= options.Count)
                    break;
                if (options.Interval <= TimeSpan.Zero) continue;
                try { await Task.Delay(options.Interval, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }
        catch (OperationCanceledException)
        {
        }

        var sent = replies.Count;
        var received = replies.Count(r => r.Status == IcmpEchoStatus.Success);
        var lost = Math.Max(0, sent - received);
        var successTimes = replies.Where(r => r.Status == IcmpEchoStatus.Success).Select(r => r.RoundtripTimeMs).ToArray();
        var protocolForbidden = replies.Exists(r => r.Status == IcmpEchoStatus.ProtocolForbidden);
        var status = DecideStatus(token.IsCancellationRequested, options.Count, sent, received, protocolForbidden);
        var (min, max, avg) = SummarizeTimes(successTimes);

        var loss = sent == 0 ? 0 : 100.0 * lost / sent;
        var result = new IcmpEchoResult(
            jobId, target, resolved, status, sent, received, lost, loss,
            min, max, avg,
            payloadRestricted, replies);

        var line = $"{status} job={jobId} target={target} sent={sent} recv={received} loss={loss:0.#} payloadRestricted={payloadRestricted}";
        if (protocolForbidden)
            NetworkLog.IcmpForbidden(line);
        else
            LogFinished(status, line);
        return result;
    }

    internal static NetworkJobStatus DecideStatus(bool cancelled, int count, int sent, int received, bool protocolForbidden)
    {
        if (cancelled && (count == 0 || sent < count))
            return NetworkJobStatus.Cancelled;
        if (received > 0)
            return NetworkJobStatus.Success;
        return protocolForbidden ? NetworkJobStatus.Failed : NetworkJobStatus.TimedOut;
    }

    internal static (long? Min, long? Max, double? Average) SummarizeTimes(IReadOnlyList<long> successTimes)
        => successTimes.Count == 0
            ? (null, null, null)
            : (successTimes.Min(), successTimes.Max(), successTimes.Average());

    internal static void LogFinished(NetworkJobStatus status, string line)
    {
        switch (status)
        {
            case NetworkJobStatus.Success:
                NetworkLog.Success(HelperLog.Subcategories.Icmp, line);
                break;
            case NetworkJobStatus.Cancelled:
                NetworkLog.Warning(HelperLog.Subcategories.Icmp, line);
                break;
            case NetworkJobStatus.Pending:
            case NetworkJobStatus.Running:
            case NetworkJobStatus.Failed:
            case NetworkJobStatus.TimedOut:
            default:
                NetworkLog.Failed(HelperLog.Subcategories.Icmp, line);
                break;
        }
    }

    private static async Task<IcmpEchoReply> SendOnceAsync(
        Ping ping, string target, byte[] buffer, int timeoutMs, PingOptions pingOptions, int sequence, CancellationToken token)
    {
        try
        {
            PingReply reply;
            try
            {
                reply = await ping.SendPingAsync(target, TimeSpan.FromMilliseconds(timeoutMs), buffer, pingOptions, token).ConfigureAwait(false);
            }
            catch (PlatformNotSupportedException) when (buffer.Length > 0)
            {
                reply = await ping.SendPingAsync(target, TimeSpan.FromMilliseconds(timeoutMs), [], pingOptions, token).ConfigureAwait(false);
                return Map(reply, sequence, payloadRestricted: true);
            }

            return Map(reply, sequence, payloadRestricted: false);
        }
        catch (OperationCanceledException) { throw; }
        catch (PingException ex)
        {
            var forbidden = IsForbidden(ex);
            return new IcmpEchoReply(
                Sequence: sequence,
                Status: forbidden ? IcmpEchoStatus.ProtocolForbidden : IcmpEchoStatus.Failed,
                Address: null, RoundtripTimeMs: 0, Ttl: 0, PayloadRestricted: false,
                Detail: ex.InnerException?.Message ?? ex.Message);
        }
        catch (SocketException ex)
        {
            return new IcmpEchoReply(sequence, IcmpEchoStatus.Failed, null, 0, 0, false, ex.Message);
        }
        catch (PlatformNotSupportedException ex)
        {
            return new IcmpEchoReply(sequence, IcmpEchoStatus.ProtocolForbidden, null, 0, 0, true, ex.Message);
        }
    }

    private static IcmpEchoReply Map(PingReply reply, int sequence, bool payloadRestricted)
    {
        var status = MapStatus(reply.Status);
        var address = MapAddress(reply.Address);
        var ttl = 0;
        try { ttl = reply.Options?.Ttl ?? 0; } catch (NotSupportedException) { }
        return new IcmpEchoReply(sequence, status, address, reply.RoundtripTime, ttl, payloadRestricted,
            reply.Status == IPStatus.Success ? null : reply.Status.ToString());
    }

    internal static IcmpEchoStatus MapStatus(IPStatus status) => status switch
    {
        IPStatus.Success => IcmpEchoStatus.Success,
        IPStatus.TimedOut => IcmpEchoStatus.TimedOut,
        IPStatus.TimeExceeded or IPStatus.TtlExpired or IPStatus.TtlReassemblyTimeExceeded => IcmpEchoStatus.TtlExpired,
        IPStatus.DestinationNetworkUnreachable or IPStatus.DestinationHostUnreachable
            or IPStatus.DestinationProtocolUnreachable or IPStatus.DestinationPortUnreachable
            or IPStatus.DestinationUnreachable => IcmpEchoStatus.DestinationUnreachable,
        _ => IcmpEchoStatus.Failed
    };

    internal static string? MapAddress(IPAddress? address)
        => address is null || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)
            ? null
            : address.ToString();

    internal static bool IsForbidden(Exception ex)
    {
        for (var cur = ex; cur is not null; cur = cur.InnerException)
        {
            if (cur is PlatformNotSupportedException) return true;
            var message = cur.Message;
            if (message.Contains("not permitted", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Access denied", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Operation not permitted", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
