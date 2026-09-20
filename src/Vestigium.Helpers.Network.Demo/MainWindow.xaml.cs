using System.IO;
using System.Text;
using System.Windows;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Network.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    void Probe_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProbeOut.Text = NetworkHelper.Probe();
        }
        catch (Exception ex)
        {
            ProbeOut.Text = ex.Message;
        }
    }

    void Subnet_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var cidr = CidrBox.Text.Trim();
            var block = NetworkHelper.DescribePrefix(cidr);
            var plan = NetworkHelper.PlanByHosts(cidr, 30);
            var vlsm = NetworkHelper.PackVlsm(cidr, [30, 14, 6]);
            SubnetOut.Text =
                $"{block}\nclass={NetworkHelper.ClassifyAddress(cidr.Split('/')[0])}\n" +
                $"plan-by-30-hosts networks={plan.Networks.Count}\n" +
                $"vlsm packs={vlsm.Networks.Count}";
        }
        catch (Exception ex)
        {
            SubnetOut.Text = ex.Message;
        }
    }

    async void Mac_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mac = NetworkHelper.ParseMac(MacBox.Text);
            var eui = NetworkHelper.ToModifiedEui64(mac);
            var sb = new StringBuilder();
            sb.AppendLine(NetworkHelper.FormatMac(mac));
            sb.AppendLine("EUI-64 " + NetworkHelper.FormatMac(eui));
            sb.AppendLine("link-local " + (NetworkHelper.ToLinkLocal(mac) ?? "—"));
            if (LiveOui.IsChecked == true)
            {
                var oui = await NetworkHelper.LookupOuiAsync(MacBox.Text).ConfigureAwait(true);
                sb.AppendLine($"OUI {oui.Source} {oui.Vendor ?? "none"}");
            }
            else
            {
                sb.AppendLine("Live OUI skipped (tick the box to opt in).");
            }

            MacOut.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            MacOut.Text = ex.Message;
        }
    }

    void Bandwidth_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!decimal.TryParse(SizeBox.Text, out var gib) || !decimal.TryParse(RateBox.Text, out var mb))
            {
                BwOut.Text = "Enter numeric size and rate.";
                return;
            }

            var size = NetworkHelper.Bandwidth(gib, DataUnit.GiB);
            var rate = NetworkHelper.Bandwidth(mb, DataUnit.Mb);
            var transfer = NetworkHelper.TransferTime(size, rate);
            var site = NetworkHelper.EstimateWebsite(new WebsiteTrafficQuery
            {
                PageSize = NetworkHelper.Bandwidth(1, DataUnit.MB),
                HumanHits = 10_000
            });
            BwOut.Text = transfer.Summary + Environment.NewLine + site.Summary + Environment.NewLine + "bots default 0";
        }
        catch (Exception ex)
        {
            BwOut.Text = ex.Message;
        }
    }

    async void Share_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "VestigiumShareProbe");
            Directory.CreateDirectory(dir);
            var root = Path.Combine(dir, "campaigns");
            Directory.CreateDirectory(root);
            NetworkTestHooks.CampaignRoot = root;
            NetworkTestHooks.ShareRoot = dir;
            var campaign = NetworkHelper.CreateShareCampaign(new ShareCampaignOptions
            {
                Target = new FileShareTarget { Directory = dir },
                PlannedSize = NetworkHelper.Bandwidth(1, DataUnit.GiB),
                ProbeBytes = 256 * 1024,
                ProbeCount = 1,
                MaxProbeBytes = 256 * 1024 * 1024,
                ResultsPath = Path.Combine(root, "demo.jsonl")
            });
            var result = await campaign.RunAsync().ConfigureAwait(true);
            ShareOut.Text =
                $"{result.Status} {result.MeasuredDuration}\n{result.Disclaimer}\nresults={Path.GetFileName(result.ResultsPath)}";
        }
        catch (Exception ex)
        {
            ShareOut.Text = ex.Message;
        }
    }
}
