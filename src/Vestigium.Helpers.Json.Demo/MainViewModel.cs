using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Json;

namespace Vestigium.Helpers.Json.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    private JsonSession? _settings;
    private JsonSession? _jsonl;

    public MainViewModel()
    {
        JsonlRows = [];
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.Json}";
        SeedDemo();
        LoadSettings();
        LoadJsonl();
    }

    public string Identity => JsonHelper.Identity;

    public string StartupSnippet =>
        "var doc = JsonHelper.Open(path);\n" +
        "doc.Snapshot();\n" +
        "doc.Set(\"network.timeoutSeconds\", 15);\n" +
        "var patch = doc.Diff();          // RFC 6902\n" +
        "doc.Commit();\n" +
        "doc.Save();                      // atomic replace";

    public string ExportFolder => JsonHelper.DefaultExportDirectory();

    public ObservableCollection<JsonlRow> JsonlRows { get; }

    [ObservableProperty] private string settingsPath = "";
    [ObservableProperty] private string jsonlPath = "";
    [ObservableProperty] private string timeoutText = "15";
    [ObservableProperty] private string levelText = "Information";
    [ObservableProperty] private string diffText = "Load settings, then Set + Diff.";
    [ObservableProperty] private string settingsCaption = "Working vs committed. Save writes committed only.";
    [ObservableProperty] private string newCode = "epsilon";
    [ObservableProperty] private string jsonlCaption = "One JSON value per line. Append at end. Save rewrites the file.";

    [RelayCommand]
    private void SeedDemo()
    {
        Directory.CreateDirectory(ExportFolder);
        SettingsPath = Path.Combine(ExportFolder, "probe-settings.json");
        JsonlPath = Path.Combine(ExportFolder, "payload-records.jsonl");
        if (!File.Exists(SettingsPath))
        {
            JsonHelper.WriteFile(SettingsPath, new
            {
                network = new { timeoutSeconds = 15 },
                logging = new { level = "Information" }
            });
        }

        if (!File.Exists(JsonlPath))
        {
            using var doc = JsonHelper.Create(JsonlPath);
            doc.AppendRecord(JsonNode.Parse("""{"code":"alpha","ms":12}""")!);
            doc.AppendRecord(JsonNode.Parse("""{"code":"beta","ms":18}""")!);
            doc.AppendRecord(JsonNode.Parse("""{"code":"gamma","ms":9}""")!);
            doc.Commit();
            doc.Save();
        }

        StatusText = $"Demo files · {ExportFolder}";
        RefreshLines();
    }

    [RelayCommand]
    private void LoadSettings()
    {
        SeedDemo();
        _settings?.Dispose();
        _settings = JsonHelper.Open(SettingsPath);
        _settings.Snapshot();
        TimeoutText = _settings.TryGet<int>("network.timeoutSeconds", out var timeout)
            ? timeout.ToString(CultureInfo.InvariantCulture)
            : "15";
        LevelText = _settings.TryGet<string>("/logging/level", out var level) ? level ?? "Information" : "Information";
        DiffText = "No pending patch.";
        SettingsCaption = $"Loaded {SettingsPath}";
        StatusText = SettingsCaption;
        RefreshLines();
    }

    [RelayCommand]
    private void ApplySettings()
    {
        var doc = SettingsSession();
        ApplyWorking(doc);
        var patch = doc.Diff();
        DiffText = FormatPatch(patch);
        SettingsCaption = $"{patch.Count} operation(s) · working only · disk unchanged";
        StatusText = SettingsCaption;
        RefreshLines();
    }

    [RelayCommand]
    private void CommitSettings()
    {
        var doc = SettingsSession();
        ApplyWorking(doc);
        var patch = doc.Diff();
        doc.Commit();
        DiffText = FormatPatch(patch) + "\n· committed, not saved";
        SettingsCaption = "Committed. Save to write the file.";
        StatusText = SettingsCaption;
        RefreshLines();
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var doc = SettingsSession();
        ApplyWorking(doc);
        doc.Commit();
        doc.Save();
        SettingsCaption = $"Saved {SettingsPath}";
        StatusText = SettingsCaption;
        RefreshLines();
    }

    [RelayCommand]
    private void CancelSettings()
    {
        var doc = SettingsSession();
        doc.Cancel();
        TimeoutText = doc.TryGet<int>("network.timeoutSeconds", out var timeout)
            ? timeout.ToString(CultureInfo.InvariantCulture)
            : "15";
        LevelText = doc.TryGet<string>("/logging/level", out var level) ? level ?? "Information" : "Information";
        DiffText = "Cancel dropped working. Disk unchanged.";
        SettingsCaption = "Cancel · disk unchanged";
        StatusText = SettingsCaption;
        RefreshLines();
    }

    [RelayCommand]
    private void LoadJsonl()
    {
        SeedDemo();
        _jsonl?.Dispose();
        _jsonl = JsonHelper.OpenJsonl(JsonlPath);
        BindJsonl(_jsonl);
        StatusText = JsonlCaption;
        RefreshLines();
    }

    [RelayCommand]
    private void AppendJsonl()
    {
        var doc = JsonlSession();
        var code = string.IsNullOrWhiteSpace(NewCode) ? "record" : NewCode.Trim();
        doc.AppendRecord(JsonNode.Parse($$"""{"code":{{JsonSerializer.Serialize(code)}}}""")!);
        BindJsonl(doc);
        StatusText = $"Appended {code} · working only until Commit + Save";
        RefreshLines();
    }

    [RelayCommand]
    private void SaveJsonl()
    {
        var doc = JsonlSession();
        doc.Commit();
        doc.Save();
        BindJsonl(doc);
        StatusText = $"Rewrote {JsonlPath} · {doc.RecordCount} record(s)";
        RefreshLines();
    }

    [RelayCommand]
    private void OpenExportFolder()
    {
        var dir = ExportFolder;
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        StatusText = dir;
    }

    [RelayCommand]
    private void RunProbe()
    {
        var id = JsonHelper.Probe();
        StatusText = $"Probe complete · Identity={id}";
        RefreshLines();
    }

    public override void Dispose()
    {
        _settings?.Dispose();
        _jsonl?.Dispose();
        base.Dispose();
    }

    private JsonSession SettingsSession()
    {
        if (_settings is null)
            LoadSettings();
        return _settings!;
    }

    private JsonSession JsonlSession()
    {
        if (_jsonl is null)
            LoadJsonl();
        return _jsonl!;
    }

    private void ApplyWorking(JsonSession doc)
    {
        if (int.TryParse(TimeoutText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeout))
            doc.Set("network.timeoutSeconds", timeout);
        doc.Set("/logging/level", LevelText.Trim());
    }

    private void BindJsonl(JsonSession doc)
    {
        JsonlRows.Clear();
        for (var i = 0; i < doc.RecordCount; i++)
        {
            var rec = doc.Record(i);
            var kind = rec is JsonObject ? "object" : rec is JsonArray ? "array" : rec is JsonValue ? "value" : "null";
            string code;
            if (rec is JsonObject obj && obj.TryGetPropertyValue("code", out var node) && node is JsonValue)
                code = node.GetValue<string>() ?? "";
            else
                code = rec?.ToJsonString() ?? "null";
            JsonlRows.Add(new JsonlRow { Index = i, Code = code, Kind = kind });
        }

        JsonlCaption = $"{doc.RecordCount} record(s) · {JsonlPath}";
    }

    private static string FormatPatch(JsonPatch patch)
        => patch.Count == 0
            ? "0 operation(s)"
            : string.Join(Environment.NewLine, patch.Operations.Select(op => $"{op.Op} {op.Path}"));
}

public sealed class JsonlRow
{
    public int Index { get; init; }
    public string Code { get; init; } = "";
    public string Kind { get; init; } = "";
}
