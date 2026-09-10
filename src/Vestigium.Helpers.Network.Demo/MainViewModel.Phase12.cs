using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Network.Demo;

public sealed partial class MainViewModel
{
    string _p95Samples = "10,10,10,10,10,12,12,20,40,100";
    string _p95Summary = "Samples are bits/s. P95 comes from Analytics. Empty list throws.";

    public string P95Samples { get => _p95Samples; set => SetProperty(ref _p95Samples, value); }
    public string P95Summary { get => _p95Summary; set => SetProperty(ref _p95Summary, value); }

    [RelayCommand]
    private void BillP95Demo()
    {
        try
        {
            var samples = P95Samples
                .Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => decimal.Parse(s.Trim()) * 1_000_000m)
                .ToArray();
            var bill = NetworkHelper.BillP95(samples);
            P95Summary = bill.Summary + Environment.NewLine + bill.Day.Summary + Environment.NewLine + bill.Days30.Summary;
            StatusText = bill.Summary;
        }
        catch (Exception ex)
        {
            P95Summary = ex.Message;
            StatusText = ex.Message;
        }
    }
}
