using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Kql.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    private KqlSession _session;
    private bool _ready;

    public MainViewModel()
    {
        Fields = [];
        Hits = [];
        Packs = Enum.GetValues<KqlPack>();
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.Kql}";
        _session = KqlHelper.Create(SelectedPack);
        QueryText = SampleQuery(SelectedPack);
        ReloadCatalog();
        _ready = true;
    }

    public string Identity => KqlHelper.Identity;
    public KqlPack[] Packs { get; }
    public ObservableCollection<KqlFieldRow> Fields { get; }
    public ObservableCollection<string> Hits { get; }

    [ObservableProperty] private KqlPack selectedPack = KqlPack.Process;
    [ObservableProperty] private string queryText = "";
    [ObservableProperty] private string compileText = "Compile binds against the selected pack.";
    [ObservableProperty] private string fixtureName = "msedge";
    [ObservableProperty] private string fixturePid = "10";

    public string PackCaption => $"Pack {SelectedPack} · {_session.Fields.Count} field(s)";

    partial void OnSelectedPackChanged(KqlPack value)
    {
        if (!_ready)
            return;
        _session.Dispose();
        _session = KqlHelper.Create(value);
        QueryText = SampleQuery(value);
        OnPropertyChanged(nameof(PackCaption));
        ReloadCatalog();
    }

    [RelayCommand]
    private void ReloadCatalog()
    {
        Fields.Clear();
        foreach (var field in _session.Fields)
        {
            Fields.Add(new KqlFieldRow
            {
                Canonical = field.Canonical,
                Type = field.Type.ToString(),
                Group = field.Group.ToString(),
                Aliases = field.Aliases.Count == 0 ? "—" : string.Join(", ", field.Aliases)
            });
        }

        OnPropertyChanged(nameof(PackCaption));
        StatusText = PackCaption;
        RefreshLines();
    }

    [RelayCommand]
    private void CompileQuery()
    {
        var compiled = KqlHelper.Compile(QueryText, _session);
        CompileText = compiled.Ok
            ? $"Compile Ok · {SelectedPack}"
            : $"{SelectedPack} · {compiled.Error}";
        StatusText = CompileText;
        RefreshLines();
    }

    [RelayCommand]
    private void RunFixture()
    {
        var compiled = KqlHelper.Compile(QueryText, _session);
        if (!compiled.Ok)
        {
            CompileText = $"{SelectedPack} · {compiled.Error}";
            StatusText = CompileText;
            RefreshLines();
            return;
        }

        var row = new KqlFixtureRow(_session);
        if (_session.TryGetField("Name", out _))
            row.Set("Name", FixtureName);
        if (int.TryParse(FixturePid, out var pid) && _session.TryGetField("PID", out _))
            row.Set("PID", pid);

        var hit = compiled.Query!.Matches(row);
        CompileText = hit ? $"Fixture matched · {SelectedPack}" : $"Fixture did not match · {SelectedPack}";
        Hits.Clear();
        Hits.Add($"fixture Name={FixtureName} PID={FixturePid} → {hit}");
        StatusText = CompileText;
        RefreshLines();
    }

    [RelayCommand]
    private void RunLive()
    {
        if (SelectedPack != KqlPack.Process)
        {
            CompileText = "Live process search requires the Process pack.";
            StatusText = CompileText;
            return;
        }

        try
        {
            var rows = ProcessHelper.Search(QueryText, ProcessDetailLevel.Identity, 64);
            Hits.Clear();
            foreach (var row in rows)
                Hits.Add($"{row.Pid}  {row.Name}");
            CompileText = $"{rows.Count} live hit(s)";
            StatusText = CompileText;
        }
        catch (Exception ex)
        {
            CompileText = ex.Message;
            StatusText = CompileText;
        }

        RefreshLines();
    }

    [RelayCommand]
    private void RunProbe()
    {
        var id = KqlHelper.Probe();
        StatusText = $"Probe complete · Identity={id}";
        RefreshLines();
    }

    public override void Dispose()
    {
        _session.Dispose();
        base.Dispose();
    }

    private static string SampleQuery(KqlPack pack) => pack switch
    {
        KqlPack.Process => "(PID == 10 || Name LIKE '%edge%') && Name LIKE '%edge%'",
        KqlPack.Service => "Name LIKE '%Win%' && Status == 'Running'",
        KqlPack.Thread => "TID > 0 && State == 'Wait'",
        KqlPack.System => "MEM.PhysicalPercent GT 50 && CPU.Usage GT 10",
        KqlPack.Adapter => "GPU.Usage GT 20 || NET.BytesSentDelta GT 0",
        _ => "PID == 0"
    };
}

public sealed class KqlFieldRow
{
    public string Canonical { get; init; } = "";
    public string Type { get; init; } = "";
    public string Group { get; init; } = "";
    public string Aliases { get; init; } = "";
}
