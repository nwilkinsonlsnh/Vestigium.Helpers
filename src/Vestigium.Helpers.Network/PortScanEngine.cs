using System.Collections.Concurrent;
using System.Net;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class PortScanEngine
{
    public static NetworkJob<PortScanResult> Create(IEnumerable<string>? hosts, PortSet ports, PortScanOptions? options)
    {
        var names = (hosts ?? []).Where(static host => !string.IsNullOrWhiteSpace(host)).Select(static host => host.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (names.Length == 0)
            throw new ArgumentException("At least one host is required.", nameof(hosts));
        if (ports.Ports.Count == 0)
            throw new ArgumentException("At least one port is required.", nameof(ports));

        var o = options ?? new PortScanOptions();
        if (!o.IncludeTcp && !o.IncludeUdp)
            throw new ArgumentException("IncludeTcp or IncludeUdp must be set.", nameof(options));
        if (o.Parallelism is < 1 or > PortScanOptions.MaxParallelism)
            throw new ArgumentOutOfRangeException(nameof(o.Parallelism), "Parallelism must be 1–64.");
        if (o.DiscoverPort is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(o.DiscoverPort), "Discover port must be 1–65535.");

        var jobId = "scan-" + HelperLog.NewId();
        return new NetworkJob<PortScanResult>(jobId, "portScan", (token, progress) => RunAsync(jobId, names, ports, o, token, progress));
    }

    private static async Task<PortScanResult> RunAsync(
        string jobId,
        string[] hosts,
        PortSet ports,
        PortScanOptions options,
        CancellationToken token,
        IProgress<NetworkProgress>? progress)
    {
        NetworkLog.Pending(HelperLog.Subcategories.Icmp, $"portScan job={jobId} hosts={hosts.Length} ports={ports.Ports.Count} discover={options.Discover}");
        using var overall = CancellationTokenSource.CreateLinkedTokenSource(token);
        overall.CancelAfter(options.OverallTimeout);
        var (live, skipped) = options.Discover
            ? await DiscoverAsync(jobId, hosts, options, progress, overall.Token).ConfigureAwait(false)
            : (hosts, Array.Empty<string>());

        var transports = (options.IncludeTcp ? 1 : 0) + (options.IncludeUdp ? 1 : 0);
        var expected = live.Length * ports.Ports.Count * transports;
        progress?.Report(new NetworkProgress { JobId = jobId, Phase = "scan", Sent = expected, Received = 0 });

        var gate = new SemaphoreSlim(options.Parallelism);
        var hostGates = live.ToDictionary(static host => host, static _ => new SemaphoreSlim(1), StringComparer.OrdinalIgnoreCase);
        var hits = new ConcurrentBag<PortScanHit>();
        var work = new List<Task>();
        var sequence = 0;

        foreach (var host in live)
        {
            foreach (var port in ports.Ports)
            {
                if (options.IncludeTcp)
                    work.Add(ProbeAsync(jobId, host, port, PortScanTransport.Tcp, options, expected, gate, hostGates[host], hits, progress, () => Interlocked.Increment(ref sequence), overall.Token));
                if (options.IncludeUdp)
                    work.Add(ProbeAsync(jobId, host, port, PortScanTransport.Udp, options, expected, gate, hostGates[host], hits, progress, () => Interlocked.Increment(ref sequence), overall.Token));
            }
        }

        try
        {
            await Task.WhenAll(work).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return Finish(jobId, hosts.Length, ports.Ports.Count, hits, skipped, NetworkJobStatus.TimedOut, options);
        }

        token.ThrowIfCancellationRequested();
        return Finish(jobId, hosts.Length, ports.Ports.Count, hits, skipped, NetworkJobStatus.Success, options);
    }

    private static async Task<(string[] Live, string[] Skipped)> DiscoverAsync(
        string jobId,
        string[] hosts,
        PortScanOptions options,
        IProgress<NetworkProgress>? progress,
        CancellationToken token)
    {
        var live = new ConcurrentBag<string>();
        var skipped = new ConcurrentBag<string>();
        var work = hosts.Select(async host =>
        {
            var up = options.DiscoverPort is int port
                ? await TcpUpAsync(host, port, options, token).ConfigureAwait(false)
                : await IcmpUpAsync(host, options, token).ConfigureAwait(false);
            (up ? live : skipped).Add(host);
            progress?.Report(new NetworkProgress { JobId = jobId, Phase = $"discover:{host}", LastStatus = up ? "Up" : "Skipped" });
        });
        await Task.WhenAll(work).ConfigureAwait(false);
        return (
            live.OrderBy(static host => host, StringComparer.OrdinalIgnoreCase).ToArray(),
            skipped.OrderBy(static host => host, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static async Task<bool> IcmpUpAsync(string host, PortScanOptions options, CancellationToken token)
    {
        var job = NetworkHelper.IcmpEcho(host, new IcmpEchoOptions
        {
            Count = 1,
            Timeout = options.ConnectTimeout,
            InterfaceIndex = options.InterfaceIndex,
            SourceAddress = options.SourceAddress
        });
        var result = await job.RunAsync(token).ConfigureAwait(false);
        return result.Received > 0;
    }

    private static async Task<bool> TcpUpAsync(string host, int port, PortScanOptions options, CancellationToken token)
    {
        var job = NetworkHelper.TcpConnect(host, port, new TcpConnectOptions
        {
            Timeout = options.ConnectTimeout,
            InterfaceIndex = options.InterfaceIndex,
            SourceAddress = options.SourceAddress,
            Family = options.Family
        });
        var result = await job.RunAsync(token).ConfigureAwait(false);
        return result.Status is TcpConnectStatus.Connected or TcpConnectStatus.Refused;
    }

    private static async Task ProbeAsync(
        string jobId,
        string host,
        int port,
        PortScanTransport transport,
        PortScanOptions options,
        int expected,
        SemaphoreSlim gate,
        SemaphoreSlim hostGate,
        ConcurrentBag<PortScanHit> hits,
        IProgress<NetworkProgress>? progress,
        Func<int> next,
        CancellationToken token)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await hostGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (options.PerHostDelay > TimeSpan.Zero)
                    await Task.Delay(options.PerHostDelay, token).ConfigureAwait(false);
            }
            finally
            {
                hostGate.Release();
            }

            var hit = transport == PortScanTransport.Tcp
                ? await ProbeTcpAsync(host, port, options, token).ConfigureAwait(false)
                : await ProbeUdpAsync(host, port, options, token).ConfigureAwait(false);
            hits.Add(hit);
            progress?.Report(new NetworkProgress
            {
                JobId = jobId,
                Phase = $"{host}:{port}/{transport}",
                Sequence = next(),
                Sent = expected,
                LastStatus = hit.Status.ToString(),
                LastRoundtripMs = hit.ElapsedMs,
                Received = hits.Count
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            hits.Add(new PortScanHit(host, port, transport, PortScanStatus.Error, null, 0, ex.GetType().Name));
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<PortScanHit> ProbeTcpAsync(string host, int port, PortScanOptions options, CancellationToken token)
    {
        var job = NetworkHelper.TcpConnect(host, port, new TcpConnectOptions
        {
            Timeout = options.ConnectTimeout,
            InterfaceIndex = options.InterfaceIndex,
            SourceAddress = options.SourceAddress,
            Family = options.Family,
            ConfirmService = options.Confirm,
            ReadBanner = options.ReadBanner
        });
        var result = await job.RunAsync(token).ConfigureAwait(false);
        var status = result.Status switch
        {
            TcpConnectStatus.Connected => PortScanStatus.Open,
            TcpConnectStatus.Refused => PortScanStatus.Closed,
            _ => PortScanStatus.TimedOut
        };
        return new PortScanHit(host, port, PortScanTransport.Tcp, status, result.Address, result.ElapsedMs, result.Detail, result.Mss, result.Banner);
    }

    private static async Task<PortScanHit> ProbeUdpAsync(string host, int port, PortScanOptions options, CancellationToken token)
    {
        var job = NetworkHelper.UdpProbe(host, port, new UdpProbeOptions
        {
            Timeout = options.ConnectTimeout,
            InterfaceIndex = options.InterfaceIndex,
            SourceAddress = options.SourceAddress,
            Family = options.Family,
            PayloadSize = 0,
            Payload = options.KnownUdp ? UdpPayloads.For(port) : null
        });
        var result = await job.RunAsync(token).ConfigureAwait(false);
        var status = result.Status switch
        {
            UdpProbeStatus.Replied => PortScanStatus.Open,
            UdpProbeStatus.Unreachable => PortScanStatus.Closed,
            _ => PortScanStatus.TimedOut
        };
        return new PortScanHit(host, port, PortScanTransport.Udp, status, result.Address, result.ElapsedMs, result.Detail);
    }

    private static PortScanResult Finish(string jobId, int hosts, int ports, IEnumerable<PortScanHit> hits, IReadOnlyList<string> skipped, NetworkJobStatus status, PortScanOptions options)
    {
        var named = PortScanRollup.Name(hits).OrderBy(static hit => hit.Host, StringComparer.OrdinalIgnoreCase).ThenBy(static hit => hit.Port).ToArray();
        var kept = options.OpenOnly
            ? named.Where(static hit => hit.Status == PortScanStatus.Open).ToArray()
            : named;
        var macs = options.ResolveMac ? ResolveMacs(named) : null;
        var rows = PortScanRollup.Hosts(named, options.Infer, macs);
        var open = named.Count(static hit => hit.Status == PortScanStatus.Open);
        NetworkLog.Success(HelperLog.Subcategories.Icmp, $"{status} portScan job={jobId} open={open} hits={kept.Length} skipped={skipped.Count}");
        return new PortScanResult(jobId, status, hosts, ports, open, kept, rows, skipped);
    }

    private static IReadOnlyDictionary<string, string?> ResolveMacs(IReadOnlyList<PortScanHit> hits)
    {
        var macs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in hits.GroupBy(static hit => hit.Host, StringComparer.OrdinalIgnoreCase))
        {
            var address = group.Select(static hit => hit.Address).FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)) ?? group.Key;
            macs[group.Key] = MacOrNull(address);
        }

        return macs;
    }

    private static string? MacOrNull(string address)
    {
        if (!IPAddress.TryParse(address, out _))
            return null;
        try
        {
            return NetworkHelper.ProbeNeighbor(address).MacAddress;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
