using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class TlsProbeEngine
{
    public static NetworkJob<TlsProbeResult> Create(string host, int port, TlsProbeOptions? options)
    {
        var name = HelperGuard.NotBlank(host, nameof(host)).Trim();
        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be 1–65535.");

        var o = options ?? new TlsProbeOptions();
        EgressBind.Validate(o.InterfaceIndex, o.SourceAddress);
        var jobId = "tls-" + HelperLog.NewId();
        return new NetworkJob<TlsProbeResult>(jobId, "tlsProbe", (token, progress) => RunAsync(jobId, name, port, o, token, progress));
    }

    private static async Task<TlsProbeResult> RunAsync(
        string jobId,
        string host,
        int port,
        TlsProbeOptions options,
        CancellationToken token,
        IProgress<NetworkProgress>? progress)
    {
        NetworkLog.Pending(HelperLog.Subcategories.Icmp, $"tlsProbe job={jobId} host={host} port={port}");
        var dest = await ResolveAsync(host, options.Family, token).ConfigureAwait(false);
        if (dest is null)
            return new TlsProbeResult(jobId, host, port, TlsProbeStatus.TimedOut, null, null, null, null, 0, "unresolved");

        var started = Stopwatch.StartNew();
        try
        {
            using var socket = new Socket(dest.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            EgressBind.Apply(socket, options.InterfaceIndex, options.SourceAddress);
            using var timed = CancellationTokenSource.CreateLinkedTokenSource(token);
            timed.CancelAfter(options.Timeout);
            await socket.ConnectAsync(dest, port, timed.Token).ConfigureAwait(false);

            using var stream = new NetworkStream(socket, ownsSocket: false);
            using var ssl = new SslStream(stream, leaveInnerStreamOpen: true, static (_, _, _, _) => true);
            var name = string.IsNullOrWhiteSpace(options.ServerName) ? host : options.ServerName.Trim();
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = name,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck
            }, timed.Token).ConfigureAwait(false);

            var cert = ssl.RemoteCertificate is null ? null : new X509Certificate2(ssl.RemoteCertificate);
            var result = new TlsProbeResult(
                jobId,
                host,
                port,
                TlsProbeStatus.Handshook,
                cert?.Subject,
                cert?.Issuer,
                cert is null ? null : new DateTimeOffset(cert.NotAfter.ToUniversalTime()),
                ssl.SslProtocol.ToString(),
                started.ElapsedMilliseconds,
                "handshook");
            progress?.Report(new NetworkProgress { JobId = jobId, Phase = "Tls", LastStatus = result.Protocol, LastRoundtripMs = result.ElapsedMs });
            NetworkLog.Success(HelperLog.Subcategories.Icmp, $"Handshook tlsProbe job={jobId} host={host} port={port} protocol={result.Protocol}");
            return result;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return new TlsProbeResult(jobId, host, port, TlsProbeStatus.TimedOut, null, null, null, null, started.ElapsedMilliseconds, "timeout");
        }
        catch (SocketException ex) when (ex.SocketErrorCode is SocketError.ConnectionRefused or SocketError.ConnectionReset)
        {
            return new TlsProbeResult(jobId, host, port, TlsProbeStatus.Refused, null, null, null, null, started.ElapsedMilliseconds, ex.SocketErrorCode.ToString());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new TlsProbeResult(jobId, host, port, TlsProbeStatus.Error, null, null, null, null, started.ElapsedMilliseconds, ex.GetType().Name);
        }
    }

    private static async Task<IPAddress?> ResolveAsync(string host, RouteFamily family, CancellationToken token)
    {
        if (IPAddress.TryParse(host, out var parsed))
            return parsed;
        try
        {
            var addrs = await Dns.GetHostAddressesAsync(host, token).ConfigureAwait(false);
            return TraceResolve.Pick(addrs, family);
        }
        catch (SocketException)
        {
            return null;
        }
    }
}
