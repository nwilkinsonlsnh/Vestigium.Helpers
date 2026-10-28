using System.Reflection;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPr04DocsTests
{
    [Fact]
    public void PR04_001_network_has_no_charts_reference()
    {
        var names = typeof(NetworkHelper).Assembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("Vestigium.Helpers.Charts", names, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("ScottPlot", names, StringComparer.OrdinalIgnoreCase);
        Assert.Null(typeof(NetworkHelper).GetMethod("ChartShare"));
        Assert.Null(typeof(NetworkHelper).GetMethod("Plot"));
    }
}
