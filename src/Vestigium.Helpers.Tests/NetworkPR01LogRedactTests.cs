using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR01LogRedactTests
{
    [Fact]
    public void PR01_009_directory_prefix_is_stripped()
    {
        Assert.Equal("evil.jsonl", NetworkLog.FileName(@"C:\Windows\evil.jsonl"));
        Assert.Equal("recipe.json", NetworkLog.FileName("/var/lib/vestigium/network/campaigns/recipe.json"));
        Assert.Equal("share.jsonl", NetworkLog.FileName(@"\\filesrv\camp\share.jsonl"));

        var windows = NetworkLog.Redact(@"recipePath not found path=C:\Temp\missing.json");
        Assert.Equal("recipePath not found path=missing.json", windows);
        Assert.DoesNotContain(@"C:\", windows);

        var unix = NetworkLog.Redact("results=/var/lib/vestigium/network/campaigns/camp-1.jsonl sent=4");
        Assert.Equal("results=camp-1.jsonl sent=4", unix);
        Assert.DoesNotContain("/var/", unix);

        Assert.Equal("dest=10.0.0.0/24", NetworkLog.Redact("dest=10.0.0.0/24"));
    }
}
