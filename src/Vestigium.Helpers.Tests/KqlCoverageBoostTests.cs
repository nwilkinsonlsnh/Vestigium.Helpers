using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class KqlCoverageBoostTests
{
    [Fact]
    public void Probe_returns_identity()
        => Assert.Equal(KqlHelper.Identity, KqlHelper.Probe());

    [Fact]
    public void Create_defaults_and_restricted_groups()
    {
        using var def = KqlHelper.Create();
        Assert.Contains(def.Fields, f => f.Canonical == "PROC.Pid");

        using var procOnly = KqlHelper.Create(new KqlOptions { Packs = [KqlPack.Process], Groups = KqlGroups.Proc });
        Assert.True(procOnly.TryGetField("Name", out _));
        Assert.False(procOnly.TryGetField("GPU.Usage", out _));

        using var adapter = KqlHelper.Create(KqlPack.Adapter);
        Assert.True(adapter.TryGetField("GPU.Usage", out _));
        Assert.False(adapter.TryGetField("PID", out _));

        using var thread = KqlHelper.Create(KqlPack.Thread);
        Assert.True(thread.TryGetField("TID", out var tid));
        Assert.Equal("THR.Tid", tid.Canonical);

        using var system = KqlHelper.Create(KqlPack.System);
        Assert.True(system.TryGetField("SYS.ProcessCount", out _));
        Assert.True(system.Fields.Any(f => f.WatchOnly));
    }

    [Fact]
    public void Parse_covers_operators_literals_and_errors()
    {
        Assert.True(KqlHelper.Parse("PID == 1 AND Name LIKE 'a*' OR NOT (Aslr == true)").Ok);
        Assert.True(KqlHelper.Parse("CPU.Time GT 1s && CPU.Time LT 2m && CPU.Time GE 1h && CPU.Time LE 1d").Ok);
        Assert.True(KqlHelper.Parse("MEM.PrivateBytes BETWEEN 1.5 AND 9").Ok);
        Assert.True(KqlHelper.Parse("Name IN ('a', \"b\")").Ok);
        Assert.True(KqlHelper.Parse("Name NOT IN ('z')").Ok);
        Assert.True(KqlHelper.Parse("Name !LIKE 'x?'").Ok);
        Assert.True(KqlHelper.Parse("PID <> 0").Ok);
        Assert.True(KqlHelper.Parse("PID != 0").Ok);
        Assert.True(KqlHelper.Parse("PID <= 1").Ok);
        Assert.True(KqlHelper.Parse("PID >= 0").Ok);
        Assert.True(KqlHelper.Parse("CPU.Time GT 10ms").Ok);
        Assert.False(KqlHelper.Parse("true == true").Ok);
        Assert.False(KqlHelper.Parse("").Ok);
        Assert.False(KqlHelper.Parse("   ").Ok);
        Assert.False(KqlHelper.Parse("A | B").Ok);
        Assert.False(KqlHelper.Parse("A & B").Ok);
        Assert.False(KqlHelper.Parse("Name == 'open").Ok);
        Assert.False(KqlHelper.Parse("Name ==").Ok);
        Assert.False(KqlHelper.Parse("== 1").Ok);
        Assert.False(KqlHelper.Parse("Name @ 1").Ok);
        Assert.False(KqlHelper.Parse("PID == 1 extra").Ok);
        Assert.False(KqlHelper.Parse("Name IN 1").Ok);
        Assert.False(KqlHelper.Parse("MEM.PrivateBytes BETWEEN 1 OR 2").Ok);
        Assert.False(KqlHelper.Parse("Name NOT EQ 'x'").Ok);
        Assert.False(KqlHelper.Parse("1 == 1").Ok);
        Assert.Contains("pipe", KqlHelper.Parse("|").Error!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("1:1 query is empty", KqlHelper.Parse("").Error!.ToString());
    }

    [Fact]
    public void Lexer_walks_tokens_including_escapes_and_suffixes()
    {
        var lexer = new KqlLexer("Name == 'a\\n\\t\\'\\\"\\\\' AND x.y >= 1");
        KqlToken token;
        var kinds = new List<KqlTokenKind>();
        do
        {
            token = lexer.Next();
            kinds.Add(token.Kind);
        } while (token.Kind != KqlTokenKind.Eof);
        Assert.Contains(KqlTokenKind.Ident, kinds);
        Assert.Contains(KqlTokenKind.Eq, kinds);
        Assert.Contains(KqlTokenKind.String, kinds);
        Assert.Contains(KqlTokenKind.And, kinds);

        Assert.Throws<KqlLexException>(() => Drain("1foo"));
        Assert.Throws<KqlLexException>(() => Drain("!"));
        Assert.Throws<KqlLexException>(() => Drain("Name."));
        Assert.Throws<KqlLexException>(() => Drain("'\\"));
        Assert.Throws<KqlLexException>(() => Drain("'a\n"));
    }

    [Fact]
    public void Like_engine_covers_wildcards_and_misses()
    {
        Assert.True(KqlLike.IsMatch("abc", "%"));
        Assert.True(KqlLike.IsMatch("abc", "*"));
        Assert.True(KqlLike.IsMatch("abc", "a*"));
        Assert.True(KqlLike.IsMatch("abc", "*c"));
        Assert.True(KqlLike.IsMatch("abc", "a?c"));
        Assert.True(KqlLike.IsMatch("AbC", "abc"));
        Assert.False(KqlLike.IsMatch("ab", "abc"));
        Assert.False(KqlLike.IsMatch("", "a"));
        Assert.True(KqlLike.IsMatch("", "%"));
        Assert.False(KqlLike.IsMatch("ab", "a?c"));
        Assert.True(KqlLike.IsMatch("axc", "a*c"));
    }

    [Fact]
    public void Evaluate_logic_in_between_types_and_unknown()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var row = new KqlRow(session)
            .Set("PID", 10)
            .Set("Name", "msedge")
            .Set("MEM.PrivateBytes", 2L)
            .Set("PROC.Aslr", true)
            .Set("CPU.Time", TimeSpan.FromSeconds(2));

        Assert.True(Eval("PID IN (9,10,11)", session, row));
        Assert.False(Eval("PID IN (1,2)", session, row));
        Assert.True(Eval("PID NOT IN (1,2)", session, row));
        Assert.False(Eval("Name NOT IN ('msedge')", session, row));
        Assert.True(Eval("MEM.PrivateBytes BETWEEN 1 AND 3", session, row));
        Assert.False(Eval("MEM.PrivateBytes BETWEEN 8 AND 9", session, row));
        Assert.True(Eval("NOT (PID == 1)", session, row));
        Assert.False(Eval("NOT (PID == 10)", session, row));
        Assert.True(Eval("Name LIKE 'ms*'", session, row));
        Assert.False(Eval("Name NOT LIKE 'ms*'", session, row));
        Assert.True(Eval("PID >= 10 && PID <= 10", session, row));
        Assert.True(Eval("PID <> 1", session, row));
        Assert.True(Eval("PROC.Aslr == true", session, row));
        Assert.False(Eval("PROC.Aslr == false", session, row));
        Assert.True(Eval("CPU.Time GT 1s", session, row));
        Assert.True(Eval("CPU.Time LE 2s", session, row));
        Assert.Equal(KqlTriState.Unknown, Compile("GPU.Usage GT 1", session).Query!.Evaluate(row));
        Assert.Equal(KqlTriState.Unknown, Compile("NOT (GPU.Usage GT 1)", session).Query!.Evaluate(row));
        Assert.Equal(KqlTriState.False, Compile("GPU.Usage GT 1 && PID == 11", session).Query!.Evaluate(row));
        Assert.Equal(KqlTriState.True, Compile("GPU.Usage GT 1 || PID == 10", session).Query!.Evaluate(row));
        Assert.Equal(KqlTriState.Unknown, Compile("GPU.Usage GT 1 || PID == 11", session).Query!.Evaluate(row));
        Assert.False(Eval("Name == ''", session, row));
        Assert.True(Eval("Name LIKE '%EDGE%'", session, row));

        var empty = new KqlRow(session);
        Assert.Equal(KqlTriState.Unknown, Compile("Name IN ('a')", session).Query!.Evaluate(empty));
        Assert.Equal(KqlTriState.Unknown, Compile("MEM.PrivateBytes BETWEEN 1 AND 2", session).Query!.Evaluate(empty));
    }

    [Fact]
    public void Binder_type_errors_and_watch_only_still_compile()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var likeInt = KqlHelper.Compile("PID LIKE '1'", session);
        Assert.False(likeInt.Ok);
        Assert.Contains("field=PROC.Pid", likeInt.Error!.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("'1'", likeInt.Error.Message, StringComparison.Ordinal);

        var between = KqlHelper.Compile("PID BETWEEN 'a' AND 'b'", session);
        Assert.False(between.Ok);
        Assert.Contains("rhs=String", between.Error!.Message, StringComparison.Ordinal);

        var inn = KqlHelper.Compile("PID IN ('x')", session);
        Assert.False(inn.Ok);

        var watch = KqlHelper.Compile("CPU.Usage GT 1", session);
        Assert.True(watch.Ok, watch.Error?.ToString());
        Assert.True(session.TryGetField("CPU.Usage", out var usage) && usage.WatchOnly);

        var bad = KqlHelper.Compile("Nope == 1", session);
        Assert.False(bad.Ok);
        Assert.Contains("unknown field", bad.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Values_from_all_clr_shapes()
    {
        Assert.True(KqlValue.From(null).IsUnknown);
        Assert.True(KqlValue.From("").IsUnknown);
        Assert.True(KqlValue.From("  ").IsUnknown);
        Assert.Equal(KqlType.Boolean, KqlValue.From(true).Type);
        Assert.Equal(KqlType.TimeSpan, KqlValue.From(TimeSpan.FromSeconds(1)).Type);
        Assert.Equal(KqlType.DateTime, KqlValue.From(DateTime.UtcNow).Type);
        Assert.Equal(KqlType.DateTime, KqlValue.From(DateTimeOffset.UtcNow).Type);
        Assert.Equal(KqlType.Integer, KqlValue.From((byte)1).Type);
        Assert.Equal(KqlType.Integer, KqlValue.From(2).Type);
        Assert.Equal(KqlType.Integer, KqlValue.From(3L).Type);
        Assert.Equal(KqlType.Integer, KqlValue.From(4u).Type);
        Assert.Equal(KqlType.Number, KqlValue.From(1.5).Type);
        Assert.Equal(KqlType.Number, KqlValue.From(1.5f).Type);
        Assert.Equal(KqlType.Number, KqlValue.From(1.5m).Type);
        Assert.Equal(KqlType.String, KqlValue.From(DayOfWeek.Monday).Type);
        var row = new KqlRow(KqlHelper.Create(KqlPack.Process));
        Assert.Throws<ArgumentException>(() => row.Set("not-a-field", 1));
        Assert.True(row.Get("PROC.Pid").IsUnknown);
    }

    [Fact]
    public void Catalog_default_groups_cover_every_pack()
    {
        Assert.True(KqlCatalog.DefaultGroups([KqlPack.Process]).HasFlag(KqlGroups.Proc));
        Assert.True(KqlCatalog.DefaultGroups([KqlPack.Service]).HasFlag(KqlGroups.Svc));
        Assert.True(KqlCatalog.DefaultGroups([KqlPack.Thread]).HasFlag(KqlGroups.Thr));
        Assert.True(KqlCatalog.DefaultGroups([KqlPack.System]).HasFlag(KqlGroups.Sys));
        Assert.True(KqlCatalog.DefaultGroups([KqlPack.Adapter]).HasFlag(KqlGroups.Gpu));
        Assert.Equal(KqlGroups.None, KqlCatalog.DefaultGroups([(KqlPack)99]));
        Assert.NotEmpty(KqlCatalog.For([], KqlGroups.None));
    }

    [Fact]
    public void Evaluator_unbound_comparison_throws()
    {
        var expr = new KqlComparisonExpression
        {
            Field = "PID",
            Op = KqlCompareOp.Eq,
            Value = new KqlLiteral { Type = KqlType.Integer, Value = 1L }
        };
        Assert.Throws<InvalidOperationException>(() => KqlEvaluator.Evaluate(expr, new KqlRow(KqlHelper.Create(KqlPack.Process))));
        Assert.Equal(KqlTriState.Unknown, KqlEvaluator.Evaluate(new DummyExpr(), new KqlRow(KqlHelper.Create(KqlPack.Process))));
    }

    private static bool Eval(string query, KqlSession session, IKqlRow row)
        => Compile(query, session).Query!.Matches(row);

    private static KqlCompileResult Compile(string query, KqlSession session)
    {
        var compiled = KqlHelper.Compile(query, session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        return compiled;
    }

    private static void Drain(string text)
    {
        var lexer = new KqlLexer(text);
        while (lexer.Next().Kind != KqlTokenKind.Eof) { }
    }

    private sealed class DummyExpr : KqlExpression;
}
