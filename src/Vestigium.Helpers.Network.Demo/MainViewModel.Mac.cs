using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Network.Demo;

public sealed partial class MainViewModel
{
    public ObservableCollection<string> MacLines { get; } = [];

    string _macInput = "001A.2B3C.4D5E";
    string _macSummary = "Parse is local. Lookup OUI is opt-in HTTPS and may be stale.";

    public string MacInput
    {
        get => _macInput;
        set => SetProperty(ref _macInput, value);
    }

    public string MacSummary
    {
        get => _macSummary;
        set => SetProperty(ref _macSummary, value);
    }

    [RelayCommand]
    private void ParseMacDemo()
    {
        try
        {
            var mac = NetworkHelper.ParseMac(MacInput);
            BindMac(mac, "parsed local (no HTTP)");
        }
        catch (Exception ex)
        {
            MacSummary = ex.Message;
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task LookupOuiDemoAsync()
    {
        try
        {
            var result = await NetworkHelper.LookupOuiAsync(MacInput, new OuiLookupOptions { Timeout = TimeSpan.FromSeconds(3) });
            MacSummary = $"{result.Source} vendor={result.Vendor ?? "—"}";
            StatusText = MacSummary;
            MacLines.Clear();
            MacLines.Add(result.Query);
            MacLines.Add(result.Disclaimer);
        }
        catch (Exception ex)
        {
            MacSummary = ex.Message;
            StatusText = ex.Message;
        }
    }

    void BindMac(MacAddress mac, string note)
    {
        MacLines.Clear();
        MacLines.Add($"colon   {mac.Colon}");
        MacLines.Add($"hyphen  {mac.Hyphen}");
        MacLines.Add($"cisco   {mac.Cisco}");
        MacLines.Add($"bare    {mac.Bare}");
        MacLines.Add($"int     {mac.Integer}");
        MacLines.Add($"eui-64  {mac.ModifiedEui64}");
        MacLines.Add($"ll      {mac.LinkLocal}");
        MacLines.Add($"oui     {mac.Oui24}  mcast={mac.IsMulticast} local={mac.IsLocallyAdministered} bcast={mac.IsBroadcast}");
        MacSummary = $"{mac.Kind} {note}";
        StatusText = MacSummary;
    }
}
