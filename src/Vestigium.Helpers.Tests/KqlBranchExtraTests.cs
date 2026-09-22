using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class KqlBranchExtraTests
{
    [Fact]
    public void Lexer_keyword_forms_and_whitespace()
    {
        var kinds = Drain("  PID GE 1 LE 2 GT 0 LT 9 AND OR NOT LIKE IN BETWEEN , ( ) \r\n");
        Assert.Contains(KqlTokenKind.Ge, kinds);
        Assert.Contains(KqlTokenKind.Le, kinds);
        Assert.Contains(KqlTokenKind.Gt, kinds);
        Assert.Contains(KqlTokenKind.Lt, kinds);
        Assert.Contains(KqlTokenKind.And, kinds);
        Assert.Contains(KqlTokenKind.Or, kinds);
        Assert.Contains(KqlTokenKind.Not, kinds);
        Assert.Contains(KqlTokenKind.Like, kinds);
        Assert.Contains(KqlTokenKind.In, kinds);
        Assert.Contains(KqlTokenKind.Between, kinds);
        Assert.Contains(KqlTokenKind.Comma, kinds);
    }

    [Fact]
    public void Parse_not_like_at_start_is_field_error()
    {
        var result = KqlHelper.Parse("NOT LIKE 'x'");
        Assert.False(result.Ok);
        Assert.Contains("field", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_in_requires_parens_and_at_least_one_value()
    {
        Assert.False(KqlHelper.Parse("Name IN ()").Ok);
        Assert.False(KqlHelper.Parse("Name IN ('a'").Ok);
        Assert.True(KqlHelper.Parse("Name IN ('a', 'b', 'c')").Ok);
    }

    [Fact]
    public void Compile_and_evaluate_ge_le_number_and_not_like()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var row = new KqlRow(session).Set("PID", 5).Set("Name", "alpha");
        Assert.True(Compile("PID GE 5", session).Query!.Matches(row));
        Assert.True(Compile("PID LE 5", session).Query!.Matches(row));
        Assert.False(Compile("PID GT 5", session).Query!.Matches(row));
        Assert.True(Compile("Name NOT LIKE 'z%'", session).Query!.Matches(row));
        Assert.False(Compile("Name !LIKE 'a%'", session).Query!.Matches(row));
        Assert.True(Compile("MEM.PrivateBytes BETWEEN 0.5 AND 1.5", session).Query!.Matches(
            new KqlRow(session).Set("MEM.PrivateBytes", 1L)));
    }

    [Fact]
    public void Evaluate_in_unknown_member_does_not_hide_hit()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = Compile("Name IN ('alpha', 'beta')", session);
        Assert.True(compiled.Query!.Matches(new KqlRow(session).Set("Name", "alpha")));
        Assert.Equal(KqlTriState.Unknown, compiled.Query.Evaluate(new KqlRow(session)));
    }

    [Fact]
    public void Create_empty_packs_defaults_to_process()
    {
        using var session = KqlHelper.Create(new KqlOptions { Packs = Array.Empty<KqlPack>() });
        Assert.True(session.TryGetField("PID", out _));
    }

    [Fact]
    public void Kql_error_to_string_and_row_set_unknown_throws()
    {
        var fail = KqlHelper.Parse("@");
        Assert.False(fail.Ok);
        Assert.Contains(":", fail.Error!.ToString());
        using var session = KqlHelper.Create(KqlPack.Service);
        Assert.Throws<ArgumentException>(() => new KqlRow(session).Set("GPU.Usage", 1));
    }

    private static KqlCompileResult Compile(string query, KqlSession session)
    {
        var compiled = KqlHelper.Compile(query, session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        return compiled;
    }

    private static List<KqlTokenKind> Drain(string text)
    {
        var lexer = new KqlLexer(text);
        var kinds = new List<KqlTokenKind>();
        KqlToken token;
        do
        {
            token = lexer.Next();
            kinds.Add(token.Kind);
        } while (token.Kind != KqlTokenKind.Eof);
        return kinds;
    }
}
