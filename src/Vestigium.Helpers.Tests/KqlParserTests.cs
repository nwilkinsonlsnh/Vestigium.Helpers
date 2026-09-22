using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class KqlParserTests
{
    [Fact]
    public void Parses_watch_example()
    {
        var result = KqlHelper.Parse("(PID == 45944 || Name LIKE 'CCleaner%') && GPU.Usage GT 20");
        Assert.True(result.Ok, result.Error?.ToString());
        var and = Assert.IsType<KqlLogicalExpression>(result.Expression);
        Assert.Equal(KqlLogicalOp.And, and.Op);
        var or = Assert.IsType<KqlLogicalExpression>(and.Left);
        Assert.Equal(KqlLogicalOp.Or, or.Op);
        var pid = Assert.IsType<KqlComparisonExpression>(or.Left);
        Assert.Equal("PID", pid.Field);
        Assert.Equal(KqlCompareOp.Eq, pid.Op);
        Assert.Equal(45944L, pid.Value.Value);
        var like = Assert.IsType<KqlComparisonExpression>(or.Right);
        Assert.Equal("Name", like.Field);
        Assert.Equal(KqlCompareOp.Like, like.Op);
        Assert.Equal("CCleaner%", like.Value.Value);
        var gpu = Assert.IsType<KqlComparisonExpression>(and.Right);
        Assert.Equal("GPU.Usage", gpu.Field);
        Assert.Equal(KqlCompareOp.Gt, gpu.Op);
        Assert.Equal(20L, gpu.Value.Value);
    }

    [Fact]
    public void Missing_value_reports_column()
    {
        var result = KqlHelper.Parse("Name ==");
        Assert.False(result.Ok);
        Assert.NotNull(result.Error);
        Assert.Equal(1, result.Error.Line);
        Assert.True(result.Error.Column >= 8, result.Error.ToString());
        Assert.Contains("expected value", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pipe_is_a_parse_error()
    {
        var result = KqlHelper.Parse("A | where B");
        Assert.False(result.Ok);
        Assert.NotNull(result.Error);
        Assert.Contains("pipe", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Not_like_parses()
    {
        var result = KqlHelper.Parse("Name NOT LIKE '%edge%'");
        Assert.True(result.Ok, result.Error?.ToString());
        var cmp = Assert.IsType<KqlComparisonExpression>(result.Expression);
        Assert.Equal(KqlCompareOp.NotLike, cmp.Op);
    }
}
