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

    public MainViewModel()
    {
        Fields = [];
        Hits = [];
        Packs = Enum.GetValues<KqlPack>();
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.Kql}";
        _session = KqlHelper.Create(SelectedPack);
        ReloadCatalog();
    }

    public string Identity => KqlHelper.Identity;
    public KqlPack[] Packs { get; }
    public ObservableCollection<KqlFieldRow> Fields { get; }
    public ObservableCollection<string> Hits { get; }

    [ObservableProperty] private KqlPack selectedPack = KqlPack.Process;
    [ObservableProperty] private string queryText = "(PID == 10 || Name LIKE '%edge%') && Name LIKE '%edge%'";
    [ObservableProperty] private string compileText = "Compile to bind against the enabled catalog.";
    [ObservableProperty] private string fixtureName = "msedge";
    [ObservableProperty] private string fixturePid = "10";

    partial void OnSelectedPackChanged(KqlPack value)
    {
        _session.Dispose();
        _session = KqlHelper.Create(value);
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

        StatusText = $"{SelectedPack} · {Fields.Count} field(s)";
        RefreshLines();
    }

    [RelayCommand]
    private void CompileQuery()
    {
        var compiled = KqlHelper.Compile(QueryText, _session);
        CompileText = compiled.Ok
            ? "Compile Ok"
            : compiled.Error?.ToString() ?? "Compile failed";
        StatusText = CompileText;
        RefreshLines();
    }

    [RelayCommand]
    private void RunFixture()
    {
        var compiled = KqlHelper.Compile(QueryText, _session);
        if (!compiled.Ok)
        {
            CompileText = compiled.Error?.ToString() ?? "Compile failed";
            StatusText = CompileText;
            RefreshLines();
            return;
        }

        var row = new KqlFixtureRow(_session).Set("Name", FixtureName);
        if (int.TryParse(FixturePid, out var pid) && _session.TryGetField("PID", out _))
            row.Set("PID", pid);

        var hit = compiled.Query!.Matches(row);
        CompileText = hit ? "Fixture matched" : "Fixture did not match (false or unknown)";
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
            StatusText = "Live process search requires the Process pack.";
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
}

public sealed class KqlFieldRow
{
    public string Canonical { get; init; } = "";
    public string Type { get; init; } = "";
    public string Group { get; init; } = "";
    public string Aliases { get; init; } = "";
}
