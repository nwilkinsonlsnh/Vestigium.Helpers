using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Json;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Network.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    public MainViewModel()
    {
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.Network} · Probe already ran at startup";
        RefreshAdapters();
    }

    public string Identity => NetworkHelper.Identity;
    public string StartupSnippet =>
        "HelperWpfHost.Start(this, HelperLog.AppIds.Network);\n" +
        "NetworkHelper.Probe();                      // on-box only\n" +
        "var job = NetworkHelper.IcmpEcho(\"127.0.0.1\");\n" +
        "var result = await job.RunAsync();";

    public ObservableCollection<AdapterRow> Adapters { get; } = [];
    public ObservableCollection<string> EchoLines { get; } = [];
    public ObservableCollection<string> TraceLines { get; } = [];
    public ObservableCollection<string> DnsLines { get; } = [];
    public ObservableCollection<ConnectionRow> Connections { get; } = [];
    public ObservableCollection<NeighborRow> Neighbors { get; } = [];
    public ObservableCollection<RouteRow> Routes { get; } = [];
    public ObservableCollection<string> CampaignLines { get; } = [];
    public ObservableCollection<string> SnapshotLines { get; } = [];

    [ObservableProperty] private string echoTarget = "127.0.0.1";
    [ObservableProperty] private string echoCountText = "4";
    [ObservableProperty] private string traceTarget = "127.0.0.1";
    [ObservableProperty] private string dnsName = "localhost";
    [ObservableProperty] private string dnsServer = "";
    [ObservableProperty] private string campaignTarget = "127.0.0.1";
    [ObservableProperty] private string campaignWindows = "00:00=1\n08:00=1";
    [ObservableProperty] private string campaignPath = "";
    [ObservableProperty] private string netBiosCaption = OperatingSystem.IsWindows() ? "Windows NetBIOS read is on Snapshot." : "NetBIOS is Windows-only.";

    [RelayCommand]
    private void RefreshAdapters()
    {
        Adapters.Clear();
        foreach (var a in NetworkHelper.GetAdapters())
        {
            var addrs = string.Join(", ", a.UnicastAddresses.Select(u =>
                u.Family == System.Net.Sockets.AddressFamily.InterNetwork
                    ? $"{u.Address}/{u.PrefixLength} ({u.SubnetMask})"
                    : $"{u.Address}/{u.PrefixLength}"));
            Adapters.Add(new AdapterRow(a.Name, a.Description, a.Status.ToString(), a.MacAddress, addrs, a.NetbiosOverTcp.ToString()));
        }
        StatusText = $"{Adapters.Count} adapter(s)";
    }

    [RelayCommand]
    private async Task RunEchoAsync()
    {
        var count = int.TryParse(EchoCountText, out var n) ? n : 4;
        EchoLines.Clear();
        var job = NetworkHelper.IcmpEcho(EchoTarget.Trim(), new IcmpEchoOptions
        {
            Count = count,
            Timeout = TimeSpan.FromSeconds(2),
            Interval = TimeSpan.FromMilliseconds(200)
        });
        var result = await job.RunAsync();
        foreach (var reply in result.Replies)
            EchoLines.Add($"#{reply.Sequence} {reply.Status} {reply.Address ?? "-"} {reply.RoundtripTimeMs} ms");
        EchoLines.Add($"{result.Status} sent={result.Sent} recv={result.Received} loss={result.LossPercent:0.#}%");
        StatusText = $"Echo {EchoTarget} · {result.Status}";
    }

    [RelayCommand]
    private async Task RunTraceAsync()
    {
        TraceLines.Clear();
        var job = NetworkHelper.IcmpTrace(TraceTarget.Trim(), new IcmpTraceOptions
        {
            MaxHops = 8,
            ProbesPerHop = 1,
            Timeout = TimeSpan.FromSeconds(1)
        });
        var result = await job.RunAsync();
        foreach (var hop in result.Hops)
            TraceLines.Add($"TTL {hop.Ttl} {hop.Address ?? "*"} {string.Join(" / ", hop.Probes.Select(p => p.Status))}");
        TraceLines.Add($"{result.Status} reached={result.Reached} protocol={result.ProbeProtocol}");
        StatusText = $"Trace {TraceTarget} · {result.Status}";
    }

    [RelayCommand]
    private async Task RunDnsAsync()
    {
        DnsLines.Clear();
        var options = new DnsLookupOptions
        {
            Type = DnsRecordType.A,
            Server = string.IsNullOrWhiteSpace(DnsServer) ? null : DnsServer.Trim(),
            Timeout = TimeSpan.FromSeconds(3)
        };
        var result = await NetworkHelper.LookupAsync(DnsName.Trim(), options);
        DnsLines.Add($"{result.Rcode} q={result.Question} server={result.Server ?? "os"} tcp={result.UsedTcp}");
        foreach (var answer in result.Answers)
            DnsLines.Add($"{answer.Type} {answer.Name} {answer.Data} ttl={answer.Ttl}");
        StatusText = $"DNS {result.Question} · {result.Rcode}";
    }

    [RelayCommand]
    private void RefreshConnections()
    {
        Connections.Clear();
        foreach (var c in NetworkHelper.GetConnections().Take(200))
            Connections.Add(new ConnectionRow(c.Protocol.ToString(), c.LocalAddress, c.LocalPort, c.RemoteAddress, c.RemotePort, c.State, c.ProcessName));
        StatusText = $"{Connections.Count} connection row(s)";
    }

    [RelayCommand]
    private void RefreshNeighbors()
    {
        Neighbors.Clear();
        foreach (var n in NetworkHelper.GetNeighbors())
            Neighbors.Add(new NeighborRow(n.Address, n.MacAddress, n.InterfaceName, n.State));
        StatusText = $"{Neighbors.Count} neighbor(s)";
    }

    [RelayCommand]
    private void RefreshRoutes()
    {
        Routes.Clear();
        foreach (var r in NetworkHelper.GetRoutes())
            Routes.Add(new RouteRow(r.Destination, r.PrefixLength, r.Mask, r.Gateway, r.InterfaceName, r.Metric));
        StatusText = $"{Routes.Count} route(s)";
    }

    [RelayCommand]
    private async Task RunCampaignAsync()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumNetworkDemo");
        Directory.CreateDirectory(dir);
        CampaignPath = Path.Combine(dir, "demo-campaign.jsonl");
        var windows = ParseWindows(CampaignWindows);
        var now = DateTime.Now;
        var campaign = NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = CampaignTarget.Trim(),
            RangeStartDate = DateOnly.FromDateTime(now.AddDays(-1)),
            RangeEndDate = DateOnly.FromDateTime(now.AddDays(1)),
            ResultsPath = CampaignPath,
            Grace = TimeSpan.FromMinutes(15),
            Echo = new IcmpEchoOptions { Timeout = TimeSpan.FromSeconds(1), Interval = TimeSpan.Zero },
            Windows = windows.Count == 0
                ? [new EchoWindow(TimeOnly.FromDateTime(now.AddMinutes(-1)), 1)]
                : windows
        });
        var result = await campaign.RunAsync();
        CampaignLines.Clear();
        CampaignLines.Add($"{result.Status} run={result.WindowsRun} missed={result.WindowsMissed} skipped={result.WindowsSkipped} echoes={result.EchoesAppended}");
        if (File.Exists(CampaignPath))
        {
            using var session = JsonHelper.OpenJsonl(CampaignPath);
            var take = Math.Min(session.RecordCount, 40);
            for (var i = Math.Max(0, session.RecordCount - take); i < session.RecordCount; i++)
                CampaignLines.Add(session.Record(i)?.ToJsonString() ?? "");
        }
        StatusText = $"Campaign {result.CampaignId} · {CampaignPath}";
    }

    [RelayCommand]
    private void RefreshSnapshot()
    {
        SnapshotLines.Clear();
        var snap = NetworkHelper.GetSnapshot();
        SnapshotLines.Add($"host={snap.Workstation.HostName} adapters={snap.Workstation.Adapters.Count}");
        SnapshotLines.Add($"routes={snap.Routes.Count} connections={snap.Connections.Count} neighbors={snap.Neighbors.Count}");
        if (OperatingSystem.IsWindows())
        {
            var nbt = NetworkHelper.GetNetBios();
            SnapshotLines.Add($"netbios host={nbt.HostName} domain={nbt.DomainName ?? "-"} adapters={nbt.Adapters.Count}");
        }
        else
        {
            SnapshotLines.Add("netbios disabled off Windows");
        }
        StatusText = "Snapshot captured";
    }

    static List<EchoWindow> ParseWindows(string text)
    {
        var rows = new List<EchoWindow>();
        foreach (var raw in text.Split(['\r', '\n', ';', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            var eq = line.IndexOf('=');
            if (eq < 0)
                continue;
            if (TimeOnly.TryParse(line[..eq].Trim(), out var time) && int.TryParse(line[(eq + 1)..].Trim(), out var count) && count > 0)
                rows.Add(new EchoWindow(time, count));
        }
        return rows;
    }
}

public sealed record AdapterRow(string Name, string Description, string Status, string? Mac, string Addresses, string Netbios);
public sealed record ConnectionRow(string Protocol, string Local, int LocalPort, string? Remote, int? RemotePort, string? State, string? Process);
public sealed record NeighborRow(string Address, string? Mac, string? InterfaceName, string State);
public sealed record RouteRow(string Destination, int Prefix, string? Mask, string Gateway, string? InterfaceName, int Metric);
