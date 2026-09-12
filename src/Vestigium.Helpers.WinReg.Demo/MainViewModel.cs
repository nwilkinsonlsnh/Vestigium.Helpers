using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.WinReg.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    public const string SandboxKey = @"Software\Vestigium\Helpers.Demo";

    public MainViewModel()
    {
        Hives = Enum.GetValues<RegistryHiveKind>();
        SearchModes = Enum.GetValues<RegistrySearchMode>();
        SelectedHive = RegistryHiveKind.CurrentUser;
        KeyPath = "Software";
        SearchMode = RegistrySearchMode.Contains;
        SearchTerm = "Vestigium";
        ValueName = "Greeting";
        ValueData = "hello";
        ExportPath = Path.Combine(Path.GetTempPath(), "vestigium-winreg-demo.reg");
        StatusText = RegistryHelper.Probe();
        EnsureSandbox();
        RefreshSandbox();
    }

    public RegistryHiveKind[] Hives { get; }
    public RegistrySearchMode[] SearchModes { get; }
    public ObservableCollection<string> SubKeys { get; } = [];
    public ObservableCollection<string> Hits { get; } = [];

    [ObservableProperty] private RegistryHiveKind selectedHive;
    [ObservableProperty] private string keyPath = "Software";
    [ObservableProperty] private string? selectedSubKey;
    [ObservableProperty] private string valueName = "Greeting";
    [ObservableProperty] private string valueData = "hello";
    [ObservableProperty] private string sandboxValue = "";
    [ObservableProperty] private string searchTerm = "Vestigium";
    [ObservableProperty] private RegistrySearchMode searchMode;
    [ObservableProperty] private string exportPath = "";

    [RelayCommand]
    private void List()
    {
        SubKeys.Clear();
        var rows = RegistryHelper.Local.ListSubKeys(SelectedHive, KeyPath, level: RegistryDetailLevel.Identity);
        foreach (var row in rows.Take(400))
            SubKeys.Add(row.Path);
        StatusText = $"Listed {rows.Count} subkeys under {SelectedHive}\\{KeyPath}";
    }

    [RelayCommand]
    private void Get()
    {
        var key = RegistryHelper.Local.GetKey(SelectedHive, KeyPath, level: RegistryDetailLevel.Slim);
        StatusText = key is null
            ? "Key not found."
            : $"{key.Hive}\\{key.Path}  subkeys={key.SubKeyCount} values={key.ValueCount}";
    }

    [RelayCommand]
    private void SetValue()
    {
        EnsureSandbox();
        var result = RegistryHelper.Local.SetValue(RegistryHiveKind.CurrentUser, SandboxKey, ValueName, ValueData, confirm: true);
        StatusText = $"{result.Status} {result.Path} name={result.ValueName}";
        RefreshSandbox();
    }

    [RelayCommand]
    private void DeleteValue()
    {
        var result = RegistryHelper.Local.DeleteValue(RegistryHiveKind.CurrentUser, SandboxKey, ValueName, confirm: true);
        StatusText = $"{result.Status} delete name={result.ValueName}";
        RefreshSandbox();
    }

    [RelayCommand]
    private void Search()
    {
        Hits.Clear();
        var hits = RegistryHelper.Search(
            SelectedHive,
            KeyPath,
            SearchTerm,
            SearchMode,
            RegistrySearchFields.KeyName | RegistrySearchFields.ValueName,
            maxDepth: 4,
            maxResults: 64);
        foreach (var hit in hits)
            Hits.Add($"{hit.MatchedOn}  {hit.Hive}\\{hit.Path}  {hit.ValueName}");
        StatusText = $"Search {hits.Count} hit(s)";
    }

    [RelayCommand]
    private void Export()
    {
        EnsureSandbox();
        var result = RegistryHelper.Export(ExportPath, RegistryHiveKind.CurrentUser, SandboxKey, RegistryExportFormat.RegFile, confirm: true);
        StatusText = $"{result.Status} {result.Reason}";
    }

    [RelayCommand]
    private void Import()
    {
        var result = RegistryHelper.Import(ExportPath, confirm: true);
        StatusText = $"{result.Status} {result.Reason}";
        RefreshSandbox();
    }

    private static void EnsureSandbox()
        => RegistryHelper.Local.CreateKey(RegistryHiveKind.CurrentUser, SandboxKey, confirm: true);

    private void RefreshSandbox()
    {
        var value = RegistryHelper.Local.GetValue(RegistryHiveKind.CurrentUser, SandboxKey, ValueName);
        SandboxValue = value is null ? "(no value)" : $"{value.Name}={value.DataText}";
    }
}
