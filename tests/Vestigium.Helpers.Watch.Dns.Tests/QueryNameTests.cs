using Vestigium.Helpers.Watch.Dns;
using Xunit;

namespace Vestigium.Helpers.Watch.Dns.Tests;

public sealed class QueryNameTests
{
    [Fact]
    public void Case_and_a_trailing_dot_are_one_key()
    {
        Assert.True(QueryName.TryNormalize("Edge.Example.", out var dotted, out _));
        Assert.True(QueryName.TryNormalize("edge.example", out var plain, out _));
        Assert.Equal(plain, dotted);
        Assert.Equal("edge.example", plain);
    }

    [Theory]
    [InlineData("https://edge.example/a")]
    [InlineData("edge.example/a")]
    [InlineData("edge..example")]
    [InlineData(".")]
    [InlineData("")]
    [InlineData("  ")]
    public void A_scheme_a_path_or_an_empty_label_rejects(string value)
    {
        Assert.False(QueryName.TryNormalize(value, out var name, out var reject));
        Assert.Equal("", name);
        Assert.False(string.IsNullOrWhiteSpace(reject));
    }
}
