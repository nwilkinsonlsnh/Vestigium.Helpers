using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Processes.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    private IProcessWatcher? _watch;
    private ISystemWatcher? _systemWatch;
    private ProcessCampaign? _campaign;

    public MainViewModel()
    {
        SearchTerm = "testhost";
        SearchMode = ProcessSearchMode.Contains;
        StartFile = System.IO.Path.Combine(Environment.SystemDirectory, "ping.exe");
        StartArguments = "-n 30 127.0.0.1";
        CampaignTerm = "testhost";
        CampaignName = "live-demo";
        StatusText = ProcessHelper.Identity;
        RefreshProcesses();
        RefreshSystem();
    }

    public ObservableCollection<ProcessInfo> Processes { get; } = [];
    public ObservableCollection<ThreadInfo> Threads { get; } = [];
    public ObservableCollection<string> SystemLines { get; } = [];
    public ObservableCollection<string> WatchLines { get; } = [];
    public ObservableCollection<string> CampaignLines { get; } = [];
    public IReadOnlyList<ProcessSearchMode> SearchModes { get; } =
        [ProcessSearchMode.StartsWith, ProcessSearchMode.EndsWith, ProcessSearchMode.Contains];

    [ObservableProperty] private string searchTerm = "";
    [ObservableProperty] private ProcessSearchMode searchMode = ProcessSearchMode.Contains;
    [ObservableProperty] private ProcessInfo? selectedProcess;
    [ObservableProperty] private string selectedDetail = "";
    [ObservableProperty] private int watchPid = Environment.ProcessId;
    [ObservableProperty] private int threadPid = Environment.ProcessId;
    [ObservableProperty] private string startFile = "";
    [ObservableProperty] private string startArguments = "";
    [ObservableProperty] private int? lastStartedPid;
    [ObservableProperty] private bool confirmKill;
    [ObservableProperty] private string campaignName = "live-demo";
    [ObservableProperty] private string campaignTerm = "";
    [ObservableProperty] private string campaignPath = "";
    [ObservableProperty] private string campaignState = "Idle";

    partial void OnSelectedProcessChanged(ProcessInfo? value)
    {
        if (value is null)
        {
            SelectedDetail = "";
            return;
        }

        WatchPid = value.Pid;
        ThreadPid = value.Pid;
        SelectedDetail =
            $"PID {value.Pid}  PPID {value.ParentPid}  {value.Name}\n" +
            $"{value.ImagePath}\n" +
            $"CPU {value.CpuTime}  Private {value.PrivateBytes:N0}  WS {value.WorkingSet:N0}\n" +
            $"Signer {value.VerifiedSigner?.Trust}  Image {value.ImageType}  Session {value.SessionId}";
    }

    [RelayCommand]
    private void RefreshProcesses()
    {
        Processes.Clear();
        IReadOnlyList<ProcessInfo> rows;
        if (string.IsNullOrWhiteSpace(SearchTerm))
            rows = ProcessHelper.List();
        else
            rows = ProcessHelper.Search(SearchTerm.Trim(), SearchMode, ProcessSearchFields.Default, ProcessDetailLevel.Slim, 256);
        foreach (var row in rows)
            Processes.Add(row);
        StatusText = $"List/Search n={rows.Count} mode={SearchMode}";
    }

    [RelayCommand]
    private void RefreshThreads()
    {
        Threads.Clear();
        if (ThreadPid <= 0)
            return;
        foreach (var row in ProcessHelper.GetThreads(ThreadPid))
            Threads.Add(row);
        StatusText = $"Threads pid={ThreadPid} n={Threads.Count}";
    }

    [RelayCommand]
    private void RefreshSystem()
    {
        var snap = ProcessHelper.GetSystemCounters();
        SystemLines.Clear();
        SystemLines.Add($"CPU % {snap.CpuPercent:0.0}   Commit {snap.CommitCurrentK:N0} K / {snap.CommitLimitK:N0} K");
        SystemLines.Add($"Physical {snap.PhysicalAvailableK:N0} K free of {snap.PhysicalTotalK:N0} K");
        SystemLines.Add($"Kernel WS {snap.KernelWorkingSet}   Cache WS {snap.CacheWorkingSet}   Nonpaged {snap.Nonpaged}");
        SystemLines.Add($"I/O reads {snap.ReadOperations}  writes {snap.WriteOperations}  other {snap.OtherOperations}");
        SystemLines.Add($"Topology cores={snap.Cores} sockets={snap.Sockets} logical={snap.LogicalProcessors}");
        SystemLines.Add($"GPU adapters={snap.GpuAdapters.Count} usage={snap.GpuUsagePercent} dedicated={snap.GpuDedicatedBytes}");
        SystemLines.Add($"Processes {snap.ProcessCount}  threads {snap.ThreadCount}  handles {snap.HandleCount}");
        StatusText = "System counters refreshed.";
    }

    [RelayCommand]
    private void StartWatch()
    {
        StopWatch();
        _watch = ProcessHelper.Watch(WatchPid, TimeSpan.FromMilliseconds(500), ProcessWatchFields.All);
        _watch.Sampled += (_, sample) => Dispatch(() =>
        {
            WatchLines.Insert(0,
                $"{sample.Timestamp:HH:mm:ss.fff} cpu={sample.CpuPercent:0.00}% Δcpu={sample.CpuTimeDelta} priv={sample.Process.PrivateBytes:N0} ws={sample.Process.WorkingSet:N0}");
            while (WatchLines.Count > 40)
                WatchLines.RemoveAt(WatchLines.Count - 1);
        });
        _watch.Exited += (_, _) => Dispatch(() => StatusText = $"Watch pid={WatchPid} exited.");
        StatusText = $"Watching pid={WatchPid}";
    }

    [RelayCommand]
    private void StopWatch()
    {
        _watch?.Dispose();
        _watch = null;
    }

    [RelayCommand]
    private void StartSystemWatch()
    {
        _systemWatch?.Dispose();
        _systemWatch = ProcessHelper.WatchSystem(TimeSpan.FromMilliseconds(500));
        _systemWatch.Sampled += (_, snap) => Dispatch(() =>
        {
            if (SystemLines.Count > 0)
                SystemLines[0] = $"{snap.Timestamp:HH:mm:ss} CPU {snap.CpuPercent:0.0}%  commit {snap.SystemCommitPercent:P0}  phys {snap.PhysicalMemoryPercent:P0}";
        });
        StatusText = "WatchSystem started.";
    }

    [RelayCommand]
    private void StopSystemWatch()
    {
        _systemWatch?.Dispose();
        _systemWatch = null;
        StatusText = "WatchSystem stopped.";
    }

    [RelayCommand]
    private void StartProcess()
    {
        var result = ProcessHelper.Start(new ProcessStartRequest
        {
            FileName = StartFile,
            Arguments = StartArguments,
            CreateNoWindow = true
        });
        LastStartedPid = result.Pid;
        StatusText = result.Ok ? $"Started pid={result.Pid}" : $"Start failed {result.Error} {result.Message}";
        RefreshProcesses();
    }

    [RelayCommand]
    private void KillSelected()
    {
        var pid = SelectedProcess?.Pid ?? LastStartedPid;
        if (pid is null or <= 0)
        {
            StatusText = "Select a process or start one first.";
            return;
        }
        if (!ConfirmKill)
        {
            StatusText = "Confirm kill is off.";
            return;
        }
        var result = ProcessHelper.Kill(pid.Value, force: true);
        StatusText = $"Kill pid={pid} {result.Status} {result.Message}";
        ConfirmKill = false;
        RefreshProcesses();
    }

    [RelayCommand]
    private void StartCampaign()
    {
        StopCampaign();
        var now = DateTime.Now;
        var campaign = ProcessHelper.CreateCampaign(new ProcessCampaignRecipe
        {
            Name = string.IsNullOrWhiteSpace(CampaignName) ? "live-demo" : CampaignName.Trim(),
            Match = new ProcessSearchRequest
            {
                Term = string.IsNullOrWhiteSpace(CampaignTerm) ? "testhost" : CampaignTerm.Trim(),
                Mode = SearchMode
            },
            SampleInterval = TimeSpan.FromSeconds(1),
            IncludeSystemCounters = true,
            Windows =
            [
                new ProcessCampaignWindow("now", TimeOnly.FromDateTime(now), TimeSpan.FromMinutes(10), ProcessCampaignDays.All)
            ]
        });
        CampaignPath = campaign.SamplePath;
        campaign.Sampled += (_, tick) => Dispatch(() =>
        {
            CampaignState = campaign.State.ToString();
            CampaignLines.Insert(0, $"{tick.Timestamp:HH:mm:ss} windows={string.Join(',', tick.OpenWindows)} n={tick.Processes.Count}");
            while (CampaignLines.Count > 30)
                CampaignLines.RemoveAt(CampaignLines.Count - 1);
        });
        _campaign = campaign;
        _ = campaign.RunAsync();
        CampaignState = campaign.State.ToString();
        StatusText = "Campaign running → " + campaign.SamplePath;
    }

    [RelayCommand]
    private void StopCampaign()
    {
        _campaign?.Dispose();
        _campaign = null;
        CampaignState = "Stopped";
    }

    public override void Dispose()
    {
        StopWatch();
        StopSystemWatch();
        StopCampaign();
        base.Dispose();
    }

    private static void Dispatch(Action action)
    {
        var app = Application.Current;
        if (app is null || app.Dispatcher.CheckAccess())
            action();
        else
            app.Dispatcher.Invoke(action);
    }
}
