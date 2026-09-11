using Vestigium.Helpers;
using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class KqlHelperTests
{
    [Fact]
    public void Identity_is_stable()
        => Assert.Equal("Vestigium.Helpers.Kql", KqlHelper.Identity);

    [Fact]
    public void Kql_appid_is_registered()
        => Assert.True(HelperLog.Taxonomy.IsSubcategoryRegistered(HelperLog.Category, HelperLog.AppIds.Kql));
}
