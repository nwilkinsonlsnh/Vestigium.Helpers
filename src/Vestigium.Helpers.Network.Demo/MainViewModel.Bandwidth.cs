using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Network.Demo;

public sealed partial class MainViewModel
{
    string _bwSize = "10";
    string _bwRate = "20";
    string _bwHuman = "10000";
    string _bwBots = "0";
    string _bwSummary = "MB is bytes. Mb is bits. Default window is a 30-day month (2,592,000 s).";

    public string BwSize { get => _bwSize; set => SetProperty(ref _bwSize, value); }
    public string BwRate { get => _bwRate; set => SetProperty(ref _bwRate, value); }
    public string BwHuman { get => _bwHuman; set => SetProperty(ref _bwHuman, value); }
    public string BwBots { get => _bwBots; set => SetProperty(ref _bwBots, value); }
    public string BwSummary { get => _bwSummary; set => SetProperty(ref _bwSummary, value); }

    [RelayCommand]
    private void ConvertBandwidthDemo()
    {
        try
        {
            var mb = NetworkHelper.Bandwidth(ParseDec(BwSize, 10), DataUnit.MB);
            var mbAsMb = NetworkHelper.ConvertBandwidth(mb, DataUnit.Mb);
            var bits = NetworkHelper.Bandwidth(ParseDec(BwSize, 10), DataUnit.Mb);
            BwSummary = $"{mb.Display} = {mbAsMb.Display}   vs   {bits.Display} (bits, not bytes)";
            StatusText = BwSummary;
        }
        catch (Exception ex)
        {
            BwSummary = ex.Message;
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private void PeriodBandwidthDemo()
    {
        try
        {
            var rate = NetworkHelper.Bandwidth(ParseDec(BwRate, 20), DataUnit.Mb);
            var month = NetworkHelper.VolumeFromRate(rate, BandwidthBasis.Days30);
            var day = NetworkHelper.VolumeFromRate(rate, BandwidthBasis.Day);
            var year = NetworkHelper.VolumeFromRate(rate, BandwidthBasis.Year365);
            BwSummary = month.Summary + Environment.NewLine + day.Summary + Environment.NewLine + year.Summary;
            StatusText = month.Summary;
        }
        catch (Exception ex)
        {
            BwSummary = ex.Message;
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private void WebsiteBandwidthDemo()
    {
        try
        {
            var bots = long.TryParse(BwBots, out var b) ? b : 0;
            var result = NetworkHelper.EstimateWebsite(new WebsiteTrafficQuery
            {
                PageSize = NetworkHelper.Bandwidth(ParseDec(BwSize, 1), DataUnit.MB),
                HumanHits = long.TryParse(BwHuman, out var h) ? h : 0,
                BotRows = bots == 0 ? [] : [new BotHitRow(CommonBotId.Googlebot, bots)]
            });
            BwSummary = result.Summary;
            StatusText = result.Summary;
        }
        catch (Exception ex)
        {
            BwSummary = ex.Message;
            StatusText = ex.Message;
        }
    }

    static decimal ParseDec(string text, decimal fallback)
        => decimal.TryParse(text, out var v) ? v : fallback;
}
