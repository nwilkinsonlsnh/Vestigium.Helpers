using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Network.Demo;

public sealed partial class MainViewModel
{
    public ObservableCollection<PrefixRow> SubnetRows { get; } = [];

    [ObservableProperty] private string subnetInput = "10.8.0.0/16";
    [ObservableProperty] private string subnetHosts = "200";
    [ObservableProperty] private string subnetNetworks = "25";
    [ObservableProperty] private string subnetVlsm = "200,50,12,2";
    [ObservableProperty] private string subnetSummary = "IPv6 host counts pick a prefix by address size (200 → /121 from a /48). Use split /64 for SLAAC.";

    [RelayCommand]
    private void ClassifySubnet()
    {
        try
        {
            var text = SubnetInput.Trim();
            var slash = text.IndexOf('/');
            var address = slash < 0 ? text : text[..slash];
            var cls = NetworkHelper.ClassifyAddress(address);
            SubnetRows.Clear();
            SubnetSummary = $"{cls.Address} class={cls.TraditionalClass} kind={cls.Kind}";
            StatusText = SubnetSummary;
        }
        catch (Exception ex)
        {
            SubnetSummary = ex.Message;
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private void DescribeSubnet()
    {
        try
        {
            var block = HasCidr(SubnetInput) ? NetworkHelper.DescribePrefix(SubnetInput.Trim()) : NetworkHelper.DescribePrefix(SubnetInput.Trim() + "/32");
            BindPlan(null, [block], $"describe {block.Network}/{block.PrefixLength} mask={block.SubnetMask ?? "—"} bcast={block.Broadcast ?? "—"}");
        }
        catch (Exception ex)
        {
            SubnetSummary = ex.Message;
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private void PlanHosts()
    {
        try
        {
            var n = int.TryParse(SubnetHosts, out var v) ? v : 200;
            var plan = NetworkHelper.PlanByHosts(RequireCidr(SubnetInput), n);
            BindPlan(plan, plan.Networks, $"{plan.Rule} child=/{plan.ChildPrefix} total={plan.TotalNetworks}");
        }
        catch (Exception ex)
        {
            SubnetSummary = ex.Message;
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private void PlanNetworks()
    {
        try
        {
            var n = int.TryParse(SubnetNetworks, out var v) ? v : 25;
            var plan = NetworkHelper.PlanByNetworks(RequireCidr(SubnetInput), n);
            BindPlan(plan, plan.Networks, $"{plan.Rule} child=/{plan.ChildPrefix} total={plan.TotalNetworks}");
        }
        catch (Exception ex)
        {
            SubnetSummary = ex.Message;
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private void PackVlsmDemo()
    {
        try
        {
            var needs = SubnetVlsm.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.Parse(s.Trim()))
                .ToArray();
            var plan = NetworkHelper.PackVlsm(RequireCidr(SubnetInput), needs);
            BindPlan(plan, plan.Networks.Concat(plan.Unused).ToList(), $"vlsm packed={plan.Networks.Count} unused={plan.Unused.Count}");
        }
        catch (Exception ex)
        {
            SubnetSummary = ex.Message;
            StatusText = ex.Message;
        }
    }

    void BindPlan(PrefixPlan? plan, IReadOnlyList<PrefixBlock> rows, string summary)
    {
        SubnetRows.Clear();
        foreach (var row in rows.Take(64))
        {
            SubnetRows.Add(new PrefixRow(
                row.Network,
                row.PrefixLength,
                row.SubnetMask ?? "—",
                row.Broadcast ?? "—",
                row.FirstUsable ?? "—",
                row.LastUsable ?? "—",
                row.UsableHosts.ToString(),
                row.TraditionalClass.ToString(),
                row.Kind.ToString()));
        }

        SubnetSummary = summary + (plan is null ? "" : $" listed={Math.Min(64, rows.Count)}");
        StatusText = SubnetSummary;
    }

    static bool HasCidr(string text) => text.Contains('/');

    static string RequireCidr(string text)
    {
        var value = text.Trim();
        if (!value.Contains('/'))
            throw new ArgumentException("Parent needs CIDR form, for example 10.8.0.0/16 or 2001:db8::/48.");
        return value;
    }
}

public sealed record PrefixRow(
    string Network,
    int Prefix,
    string Mask,
    string Broadcast,
    string First,
    string Last,
    string Hosts,
    string Class,
    string Kind);
