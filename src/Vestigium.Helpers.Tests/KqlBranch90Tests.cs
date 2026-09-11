using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class KqlBranch90Tests
{
    [Fact]
    public void Helper_probe_groups_and_parse_failures()
    {
        Assert.Equal(KqlHelper.Identity, KqlHelper.Probe());
        using var cpu = KqlHelper.Create(new KqlOptions { Packs = [KqlPack.Process], Groups = KqlGroups.Cpu });
        Assert.True(cpu.TryGetField("CPU.Usage", out _));
        Assert.False(cpu.TryGetField("PROC.Name", out _));
        using var adapter = KqlHelper.Create(KqlPack.Adapter);
        Assert.True(adapter.TryGetField("GPU.Usage", out _));
        using var service = KqlHelper.Create(KqlPack.Service);
        Assert.True(service.TryGetField("SVC.Name", out _));
        using var thread = KqlHelper.Create(KqlPack.Thread);
        Assert.True(thread.TryGetField("TID", out _));

        Assert.False(KqlHelper.Parse("").Ok);
        Assert.False(KqlHelper.Parse("   ").Ok);
        Assert.False(KqlHelper.Parse("PID == 1 leftover").Ok);
        Assert.False(KqlHelper.Parse("(PID == 1").Ok);
        Assert.False(KqlHelper.Parse("PID").Ok);
        Assert.False(KqlHelper.Parse("PID BETWEEN 1").Ok);
        Assert.False(KqlHelper.Parse("Name NOT foo").Ok);
        Assert.False(KqlHelper.Parse("Name IN 'a'").Ok);
        Assert.False(KqlHelper.Parse("| summarize").Ok);
        Assert.False(KqlHelper.Parse("PID & 1").Ok);
        Assert.False(KqlHelper.Parse("PID ! 1").Ok);
        Assert.False(KqlHelper.Parse("'unterminated").Ok);
        Assert.False(KqlHelper.Parse("1xyz == 1").Ok);
        Assert.False(KqlHelper.Parse("CPU.").Ok);
        using var session = KqlHelper.Create(KqlPack.Process);
        Assert.False(KqlHelper.Compile("", session).Ok);
        Assert.False(KqlHelper.Compile("PID LIKE 'x'", session).Ok);
        Assert.False(KqlHelper.Compile("PID == 'x'", session).Ok);
        Assert.False(KqlHelper.Compile("Name LIKE 1", session).Ok);
        Assert.True(KqlHelper.Compile("Name == 'a%*?'", session).Ok);
        Assert.True(KqlHelper.Compile("Name != 'x*'", session).Ok);
    }

    [Fact]
    public void Lexer_and_like_cover_remaining_tokens()
    {
        Assert.Contains(KqlTokenKind.Or, Drain("a || b"));
        Assert.Contains(KqlTokenKind.And, Drain("a && b"));
        Assert.Contains(KqlTokenKind.Ne, Drain("a <> b"));
        Assert.Contains(KqlTokenKind.Ne, Drain("a != b"));
        Assert.Contains(KqlTokenKind.Le, Drain("a <= b"));
        Assert.Contains(KqlTokenKind.Ge, Drain("a >= b"));
        Assert.Contains(KqlTokenKind.NotLike, Drain("a !LIKE 'x'"));
        Assert.Contains(KqlTokenKind.TimeSpan, Drain("1ms 2s 3m 4h 5d"));
        Assert.Contains(KqlTokenKind.Number, Drain(".5"));
        Assert.Contains(KqlTokenKind.String, Drain("\"q\""));
        Assert.Contains(KqlTokenKind.String, Drain("'\\n\\t\\'\\\"\\\\x'"));
        Assert.Contains(KqlTokenKind.True, Drain("TRUE"));
        Assert.Contains(KqlTokenKind.False, Drain("false"));
        Drain(null!);

        Assert.True(KqlLike.IsMatch("abc", "%"));
        Assert.True(KqlLike.IsMatch("abc", "*"));
        Assert.True(KqlLike.IsMatch("abc", "a%"));
        Assert.True(KqlLike.IsMatch("abc", "%c"));
        Assert.True(KqlLike.IsMatch("abc", "a*c"));
        Assert.True(KqlLike.IsMatch("abc", "a?c"));
        Assert.True(KqlLike.IsMatch("abc", "%%*"));
        Assert.True(KqlLike.IsMatch("AbC", "abc"));
        Assert.False(KqlLike.IsMatch("abc", "a?c?x"));
        Assert.False(KqlLike.IsMatch("ab", "abc"));
        Assert.False(KqlLike.IsMatch("abc", "abd"));
        Assert.True(KqlLike.IsMatch("", ""));
        Assert.False(KqlLike.IsMatch("", "a"));
        Assert.True(KqlLike.IsMatch("", "%"));
    }

    [Fact]
    public void Evaluator_covers_logic_types_and_literals()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var row = new KqlRow(session)
            .Set("PID", 10)
            .Set("Name", "Alpha.exe")
            .Set("PROC.Aslr", true)
            .Set("CPU.Time", TimeSpan.FromSeconds(2))
            .Set("MEM.PrivateBytes", 5L);

        Assert.True(Ok("PID == 10", session, row));
        Assert.True(Ok("PID <> 1", session, row));
        Assert.True(Ok("PID != 1", session, row));
        Assert.True(Ok("PID < 11 && PID > 9", session, row));
        Assert.True(Ok("PID <= 10 || PID >= 99", session, row));
        Assert.False(Ok("PID == 1 && Name == 'nope'", session, row));
        Assert.True(Ok("PID == 1 || Name LIKE 'a%'", session, row));
        Assert.True(Ok("NOT (PID == 1)", session, row));
        Assert.True(Ok("Name NOT IN ('z')", session, row));
        Assert.False(Ok("Name IN ('z')", session, row));
        Assert.True(Ok("Name NOT LIKE 'z%'", session, row));
        Assert.True(Ok("MEM.PrivateBytes BETWEEN 1 AND 9", session, row));
        Assert.False(Ok("MEM.PrivateBytes BETWEEN 50 AND 60", session, row));
        Assert.True(Ok("PROC.Aslr == true", session, row));
        Assert.False(Ok("PROC.Aslr == false", session, row));
        Assert.True(Ok("CPU.Time BETWEEN 1s AND 3s", session, row));
        Assert.True(Ok("CPU.Time BETWEEN 1ms AND 1h", session, row));
        Assert.True(Ok("CPU.Time BETWEEN 1m AND 1d", session, row));
        Assert.Equal(KqlTriState.Unknown, Comp("CommandLine LIKE '%x%'", session).Query!.Evaluate(row));
        Assert.Equal(KqlTriState.Unknown, Comp("NOT (CommandLine == 'x')", session).Query!.Evaluate(row));
        Assert.Throws<ArgumentNullException>(() => Comp("PID == 1", session).Query!.Evaluate(null!));

        Assert.True(KqlValue.From((byte)1).Type == KqlType.Integer);
        Assert.True(KqlValue.From((sbyte)1).Type == KqlType.Integer);
        Assert.True(KqlValue.From((short)1).Type == KqlType.Integer);
        Assert.True(KqlValue.From((ushort)1).Type == KqlType.Integer);
        Assert.True(KqlValue.From(1u).Type == KqlType.Integer);
        Assert.True(KqlValue.From(1ul).Type == KqlType.Integer);
        Assert.True(KqlValue.From(1.5f).Type == KqlType.Number);
        Assert.True(KqlValue.From(1.5m).Type == KqlType.Number);
        Assert.True(KqlValue.From(DateTime.UtcNow).Type == KqlType.DateTime);
        Assert.True(KqlValue.From(DateTimeOffset.UtcNow).Type == KqlType.DateTime);
        Assert.True(KqlValue.From(Guid.NewGuid()).Type == KqlType.String);
        Assert.True(KqlValue.From("  ").IsUnknown);
        Assert.True(KqlValue.From((string?)null).IsUnknown);
        Assert.True(KqlValue.From(true).Type == KqlType.Boolean);
    }

    [Fact]
    public void Parser_not_in_and_timespan_units()
    {
        Assert.True(KqlHelper.Parse("Name NOT IN ('a', 'b')").Ok);
        Assert.True(KqlHelper.Parse("Name !LIKE 'x'").Ok);
        Assert.True(KqlHelper.Parse("(PID == 1 OR PID == 2) AND Name LIKE 'a%'").Ok);
        Assert.True(KqlHelper.Parse("NOT NOT PID == 1").Ok);
        Assert.False(KqlHelper.Parse("PID BETWEEN 1 OR 2").Ok);
        using var session = KqlHelper.Create(KqlPack.Process);
        Assert.True(Comp("CPU.Time BETWEEN 10ms AND 2d", session).Ok);
    }

    private static bool Ok(string query, KqlSession session, IKqlRow row)
        => Comp(query, session).Query!.Matches(row);

    private static KqlCompileResult Comp(string query, KqlSession session)
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
