using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ServicePhase8Tests : IDisposable
{
    public ServicePhase8Tests()
    {
        ServiceTestHooks.CampaignRoot = Path.Combine(Path.GetTempPath(), "vest-svc8-" + Guid.NewGuid().ToString("N"));
        ServiceTestHooks.Now = () => new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);
    }

    public void Dispose()
    {
        var root = ServiceTestHooks.CampaignRoot;
        ServiceTestHooks.CampaignRoot = null;
        ServiceTestHooks.Now = null;
        if (root is not null && Directory.Exists(root))
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void Snapshots_have_no_password_property()
    {
        Assert.DoesNotContain(typeof(ServiceInfo).GetProperties(), p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(ServiceControlResult).GetProperties(), p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(ServiceRecoveryInfo).GetProperties(), p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(ServiceCampaignTick).GetProperties(), p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(typeof(ServiceLogonRequest).GetProperties(), p => p.Name == "Password");
    }

    [Fact]
    public void Probe_matches_identity()
        => Assert.Equal("Vestigium.Helpers.Services", ServiceHelper.Probe());

    [Fact]
    public void Get_blank_throws()
        => Assert.Throws<ArgumentException>(() => ServiceHelper.Get(" "));

    [Fact]
    public void WatchQuery_bad_kql_throws_at_construct()
        => Assert.Throws<ArgumentException>(() => ServiceHelper.WatchQuery("WindowTitle == 'x'", TimeSpan.FromSeconds(1)));

    [Fact]
    public void CreateCampaign_overwrites_recipe()
    {
        var first = ServiceHelper.CreateCampaign(Recipe("dup"));
        var again = ServiceHelper.CreateCampaign(Recipe("dup"));
        Assert.Equal(first.RecipePath, again.RecipePath);
        Assert.True(File.Exists(again.RecipePath));
    }

    [Fact]
    public void LoadCampaign_round_trips()
    {
        ServiceHelper.CreateCampaign(Recipe("round"));
        using var loaded = ServiceHelper.LoadCampaign("round");
        Assert.Equal("round", loaded.CampaignId);
        Assert.Equal("Name LIKE 'Event%'", loaded.Recipe.Query);
    }

    [Fact]
    public void LoadCampaign_missing_throws()
        => Assert.Throws<FileNotFoundException>(() => ServiceHelper.LoadCampaign("no-such-campaign"));

    [Fact]
    public void Protected_names_block_writes()
    {
        Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.Stop("EventLog", confirmDependents: true).Status);
        Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.SetStartType("EventLog", ServiceStartType.Manual, confirm: true).Status);
        Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.SetLogon("EventLog", new ServiceLogonRequest { Confirm = true }).Status);
        Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.SetRecovery("EventLog", new ServiceRecoveryRequest { Confirm = true }).Status);
    }

    [Fact]
    public void Default_campaign_root_is_under_vestigium_when_unhooked()
    {
        var hooked = ServiceTestHooks.CampaignRoot;
        ServiceTestHooks.CampaignRoot = null;
        try
        {
            Assert.Contains("Vestigium", ServiceHelper.DefaultCampaignRoot, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Services", ServiceHelper.DefaultCampaignRoot, StringComparison.OrdinalIgnoreCase);
        }
        finally { ServiceTestHooks.CampaignRoot = hooked; }
    }

    private static ServiceCampaignRecipe Recipe(string name) => new()
    {
        Name = name,
        Query = "Name LIKE 'Event%'",
        SampleInterval = TimeSpan.FromSeconds(1),
        TimeZoneId = "UTC",
        Windows = [new ServiceCampaignWindow("noon", new TimeOnly(12, 0), TimeSpan.FromMinutes(5), ServiceCampaignDays.All)]
    };
}
