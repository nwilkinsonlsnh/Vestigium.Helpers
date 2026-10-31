using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Network.RawSocket;

internal static class SynScanEngine
{
    public static RawSocketJob<SynScanResult> Create(IEnumerable<string>? hosts, PortSet ports, SynScanOptions? options)
    {
        var names = (hosts ?? []).Where(static host => !string.IsNullOrWhiteSpace(host)).Select(static host => host.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (names.Length == 0)
            throw new ArgumentException("At least one host is required.", nameof(hosts));
        if (names.Length > SynScanOptions.MaxHosts)
            throw new ArgumentOutOfRangeException(nameof(hosts), $"Host list is capped at {SynScanOptions.MaxHosts}.");
        if (ports.Ports.Count == 0)
            throw new ArgumentException("At least one port is required.", nameof(ports));

        var o = options ?? new SynScanOptions();
        if (o.Parallelism is < 1 or > SynScanOptions.MaxParallelism)
            throw new ArgumentOutOfRangeException(nameof(o.Parallelism), "Parallelism must be 1–32.");
        EgressBind.Validate(o.InterfaceIndex, o.SourceAddress);

        var jobId = "syn-" + Guid.NewGuid().ToString("N")[..8];
        return new RawSocketJob<SynScanResult>(jobId, "synScan", (token, progress) => RunAsync(jobId, names, ports, o, token, progress));
    }

    private static async Task<SynScanResult> RunAsync(string jobId, string[] hosts, PortSet ports, SynScanOptions options, CancellationToken token, IProgress<NetworkProgress>? progress)
    {
        var expected = hosts.Length * ports.Ports.Count;
        progress?.Report(new NetworkProgress { JobId = jobId, Phase = "scan", Sent = expected, Received = 0 });

        var access = RawSocketRights.Check();
        if (!access.IsGranted)
            return new SynScanResult(jobId, SynScanStatus.Denied, access.Detail, hosts.Length, ports.Ports.Count, 0, []);

        using var overall = CancellationTokenSource.CreateLinkedTokenSource(token);
        overall.CancelAfter(options.OverallTimeout);
        var hits = new ConcurrentBag<SynScanHit>();
        var finished = 0;
        var gate = new SemaphoreSlim(options.Parallelism);
        try
        {
            var work = new List<Task>();
            foreach (var host in hosts)
            {
                var dest = await ResolveAsync(host, overall.Token).ConfigureAwait(false);
                if (dest is null)
                {
                    foreach (var port in ports.Ports)
                    {
                        hits.Add(new SynScanHit(host, port, SynPortStatus.Filtered, 0, "unresolved"));
                        var done = Interlocked.Increment(ref finished);
                        progress?.Report(new NetworkProgress { JobId = jobId, Phase = $"{host}:{port}", Sent = expected, Received = done, LastStatus = "unresolved" });
                    }
                    continue;
                }

                var local = LocalAddress(dest, options);
                foreach (var port in ports.Ports)
                    work.Add(ProbeGatedAsync(jobId, host, local, dest, port, options, expected, gate, hits, progress, () => Interlocked.Increment(ref finished), overall.Token));
            }

            await Task.WhenAll(work).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return Finish(jobId, hosts.Length, ports.Ports.Count, hits, SynScanStatus.TimedOut, "timeout");
        }
        finally
        {
            gate.Dispose();
        }

        token.ThrowIfCancellationRequested();
        return Finish(jobId, hosts.Length, ports.Ports.Count, hits, SynScanStatus.Success, null);
    }

    private static async Task ProbeGatedAsync(
        string jobId,
        string host,
        IPAddress local,
        IPAddress dest,
        int port,
        SynScanOptions options,
        int expected,
        SemaphoreSlim gate,
        ConcurrentBag<SynScanHit> hits,
        IProgress<NetworkProgress>? progress,
        Func<int> next,
        CancellationToken token)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            using var socket = OpenRaw(options);
            var hit = socket is null
                ? new SynScanHit(host, port, SynPortStatus.Filtered, 0, "raw-tcp-unavailable")
                : await ProbeAsync(socket, host, local, dest, port, options, token).ConfigureAwait(false);
            hits.Add(hit);
            progress?.Report(new NetworkProgress
            {
                JobId = jobId,
                Phase = $"{host}:{port}",
                Sent = expected,
                Received = next(),
                LastStatus = hit.Status.ToString(),
                LastRoundtripMs = hit.ElapsedMs
            });
        }
        finally
        {
            gate.Release();
        }
    }

    private static Socket? OpenRaw(SynScanOptions options)
    {
        try
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Tcp);
            socket.ReceiveTimeout = 1000;
            EgressBind.Apply(socket, options.InterfaceIndex, options.SourceAddress);
            return socket;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    private static async Task<SynScanHit> ProbeAsync(Socket socket, string host, IPAddress local, IPAddress dest, int port, SynScanOptions options, CancellationToken token)
    {
        var started = Stopwatch.StartNew();
        var localPort = 40000 + Random.Shared.Next(20000);
        var seq = (uint)Random.Shared.Next();
        var syn = SynPacket.Build(local, dest, localPort, port, seq, 0x02);
        await socket.SendToAsync(syn, SocketFlags.None, new IPEndPoint(dest, port), token).ConfigureAwait(false);

        var buffer = new byte[128];
        using var timed = CancellationTokenSource.CreateLinkedTokenSource(token);
        timed.CancelAfter(options.Timeout);
        try
        {
            while (!timed.IsCancellationRequested)
            {
                var got = await socket.ReceiveAsync(buffer, SocketFlags.None, timed.Token).ConfigureAwait(false);
                if (!SynPacket.TryClassify(buffer, got, localPort, port, out var status))
                    continue;
                if (status == SynPortStatus.Open)
                    await SendRstAsync(socket, local, dest, localPort, port, seq + 1, token).ConfigureAwait(false);
                return new SynScanHit(host, port, status, started.ElapsedMilliseconds, status.ToString().ToLowerInvariant());
            }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
        }
        catch (SocketException)
        {
        }

        return new SynScanHit(host, port, SynPortStatus.Filtered, started.ElapsedMilliseconds, "filtered");
    }

    private static Task SendRstAsync(Socket socket, IPAddress local, IPAddress dest, int localPort, int port, uint ack, CancellationToken token)
    {
        var rst = SynPacket.Build(local, dest, localPort, port, ack, 0x04);
        return socket.SendToAsync(rst, SocketFlags.None, new IPEndPoint(dest, port), token).AsTask();
    }

    private static IPAddress LocalAddress(IPAddress dest, SynScanOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SourceAddress) && IPAddress.TryParse(options.SourceAddress.Trim(), out var pinned))
            return pinned;

        using var probe = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        EgressBind.Apply(probe, options.InterfaceIndex, options.SourceAddress);
        probe.Connect(new IPEndPoint(dest, 9));
        return ((IPEndPoint)probe.LocalEndPoint!).Address;
    }

    private static async Task<IPAddress?> ResolveAsync(string host, CancellationToken token)
    {
        if (IPAddress.TryParse(host, out var parsed))
            return parsed.AddressFamily == AddressFamily.InterNetwork ? parsed : null;
        try
        {
            var addrs = await Dns.GetHostAddressesAsync(host, AddressFamily.InterNetwork, token).ConfigureAwait(false);
            return addrs.FirstOrDefault();
        }
        catch (SocketException)
        {
            return null;
        }
    }

    private static SynScanResult Finish(string jobId, int hosts, int ports, IEnumerable<SynScanHit> hits, SynScanStatus status, string? detail)
    {
        var list = hits.OrderBy(static hit => hit.Host, StringComparer.OrdinalIgnoreCase).ThenBy(static hit => hit.Port).ToArray();
        var open = list.Count(static hit => hit.Status == SynPortStatus.Open);
        return new SynScanResult(jobId, status, detail, hosts, ports, open, list);
    }
}
