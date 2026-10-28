using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPr03CloseTests
{
    [Fact]
    public void PR03_010_share_surface_has_no_password_and_no_writeprobe()
    {
        Assert.Null(typeof(FileShareTarget).GetProperty("Password"));
        Assert.Null(typeof(ShareCampaignOptions).GetProperty("Password"));
        Assert.Null(typeof(NetworkHelper).GetMethod("WriteProbe"));
        Assert.Null(typeof(NetworkHelper).GetMethod("ReadProbe"));
        Assert.NotNull(typeof(NetworkHelper).GetMethod(nameof(NetworkHelper.PlanShareProbe)));
        Assert.NotNull(typeof(NetworkHelper).GetMethod(nameof(NetworkHelper.CreateShareCampaign)));
        Assert.NotNull(typeof(NetworkHelper).GetMethod(nameof(NetworkHelper.OpenShareCampaign)));
    }

    [Fact]
    public void PR03_010_result_splits_payload_and_metadata()
    {
        var ctor = typeof(ShareCampaignResult).GetConstructors()[0];
        var names = ctor.GetParameters().Select(p => p.Name).ToArray();
        Assert.Contains("PayloadDuration", names);
        Assert.Contains("MetadataDuration", names);
        Assert.Contains("DeclaredPipeDuration", names);
    }
}
