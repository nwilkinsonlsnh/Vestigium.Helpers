namespace Vestigium.Helpers.Kql;

internal static class KqlCompleter
{
    public static KqlCompletion Complete(string? text, int caret, KqlSession session, IReadOnlyList<string>? hints)
    {
        text ??= string.Empty;
        if (caret < 0)
            return KqlCompletion.Empty;
        if (caret > text.Length)
            caret = text.Length;

        var head = text[..caret];
        if (InsideConstructor(head))
            return new KqlCompletion { Slot = KqlCompletionSlot.None };

        var partialStart = PartialStart(head);
        var partial = head[partialStart..];
        var finished = head[..partialStart];
        if (!TryTokens(finished, out var tokens))
            return KqlCompletion.Empty;

        FoldPair(tokens, ref partial, ref partialStart);
        var slot = SlotOf(tokens, session, out var field);
        var rows = Rank(Rows(slot, field, session), partial, hints);
        return new KqlCompletion
        {
            Slot = slot,
            Rows = rows,
            ReplaceStart = partialStart,
            ReplaceLength = caret - partialStart
        };
    }

    private static KqlCompletionRow[] Rank(IEnumerable<KqlCompletionRow> rows, string partial, IReadOnlyList<string>? hints)
    {
        var saved = hints ?? [];
        return rows
            .Select((row, index) => new { row, index, rank = RankOf(row.Insert, partial) })
            .Where(item => item.rank > 0)
            .OrderByDescending(item => item.rank)
            .ThenByDescending(item => item.rank == 2 && Seen(item.row.Insert, saved))
            .ThenBy(item => item.index)
            .Select(item => item.row)
            .ToArray();
    }

    private static int RankOf(string insert, string partial)
    {
        if (partial.Length == 0 || insert.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            return 2;
        return insert.Contains(partial, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    private static bool Seen(string insert, IReadOnlyList<string> hints)
    {
        foreach (var hint in hints)
        {
            if (!string.IsNullOrWhiteSpace(hint) && hint.Contains(insert, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void FoldPair(List<KqlToken> tokens, ref string partial, ref int partialStart)
    {
        if (tokens.Count == 0 || tokens[^1].Kind != KqlTokenKind.Ident)
            return;
        var word = tokens[^1].Text;
        if (!word.Equals("BEGINS", StringComparison.OrdinalIgnoreCase) && !word.Equals("ENDS", StringComparison.OrdinalIgnoreCase))
            return;
        if (partial.Length > 0 && !"WITH".StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            return;
        tokens.RemoveAt(tokens.Count - 1);
        partial = word + " " + partial;
        partialStart -= word.Length;
    }

    private static KqlCompletionSlot SlotOf(IReadOnlyList<KqlToken> tokens, KqlSession session, out KqlField? field)
    {
        field = null;
        if (tokens.Count == 0)
            return KqlCompletionSlot.Field;

        var last = tokens[^1];
        if (last.Kind is KqlTokenKind.And or KqlTokenKind.Or or KqlTokenKind.Not or KqlTokenKind.LParen)
            return KqlCompletionSlot.Field;
        if (last.Kind == KqlTokenKind.Ident && session.TryGetField(last.Text, out field!))
            return KqlCompletionSlot.Operator;
        if (IsOperator(last.Kind) || IsPair(last))
        {
            field = FieldBefore(tokens, session);
            return KqlCompletionSlot.Value;
        }
        if (IsValue(last.Kind) || last.Kind == KqlTokenKind.RParen)
            return KqlCompletionSlot.Join;
        return KqlCompletionSlot.None;
    }

    private static IEnumerable<KqlCompletionRow> Rows(KqlCompletionSlot slot, KqlField? field, KqlSession session)
    {
        switch (slot)
        {
            case KqlCompletionSlot.Field:
                foreach (var item in session.Fields)
                {
                    yield return Row(item.Canonical, KqlCompletionKind.Field);
                    var dot = item.Canonical.LastIndexOf('.');
                    if (dot < 0)
                        continue;
                    var suffix = item.Canonical[(dot + 1)..];
                    if (session.Fields.Count(other => other.Canonical.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase)) == 1)
                        yield return Row(suffix, KqlCompletionKind.Field);
                }
                yield break;
            case KqlCompletionSlot.Operator:
                foreach (var op in Operators(field))
                    yield return Row(op, KqlCompletionKind.Operator);
                yield break;
            case KqlCompletionSlot.Value:
                if (field is null)
                    yield break;
                foreach (var value in field.Closed)
                {
                    yield return Row(field.Canonical + "." + value, KqlCompletionKind.Value);
                    yield return Row(field.Canonical + "(" + value + ")", KqlCompletionKind.Value);
                }
                foreach (var ctor in Constructors(field))
                    yield return Row(ctor, KqlCompletionKind.Constructor);
                if (field.Type == KqlType.Boolean)
                {
                    yield return Row("true", KqlCompletionKind.Value);
                    yield return Row("false", KqlCompletionKind.Value);
                }
                yield break;
            case KqlCompletionSlot.Join:
                yield return Row("&&", KqlCompletionKind.Join);
                yield return Row("||", KqlCompletionKind.Join);
                yield return Row("AND", KqlCompletionKind.Join);
                yield return Row("OR", KqlCompletionKind.Join);
                yield return Row(")", KqlCompletionKind.Join);
                yield break;
        }
    }

    private static IEnumerable<string> Operators(KqlField? field)
    {
        if (field is null)
            yield break;
        if (field.Type == KqlType.Boolean && field.CompareAs == KqlCompareAs.Stored)
        {
            yield return "==";
            yield return "!=";
            yield break;
        }

        yield return "==";
        yield return "!=";
        if (field.CompareAs is KqlCompareAs.IpAddress or KqlCompareAs.MacAddress)
        {
            yield return "CONTAINS";
            yield return "BEGINS WITH";
            yield return "ENDS WITH";
            yield break;
        }

        if (field.Type is KqlType.Integer or KqlType.Number)
        {
            yield return "<";
            yield return ">";
            yield return "<=";
            yield return ">=";
            yield return "GT";
            yield return "LT";
            yield return "GTE";
            yield return "LTE";
            yield return "BETWEEN";
        }

        yield return "CONTAINS";
        yield return "BEGINS WITH";
        yield return "ENDS WITH";
        yield return "LIKE";
    }

    private static IEnumerable<string> Constructors(KqlField field)
    {
        switch (field.CompareAs)
        {
            case KqlCompareAs.IpAddress:
                yield return "ipaddress(";
                yield break;
            case KqlCompareAs.MacAddress:
                yield return "macaddress(";
                yield break;
        }

        if (field.Type == KqlType.String)
            yield return "string(";
    }

    private static KqlField? FieldBefore(IReadOnlyList<KqlToken> tokens, KqlSession session)
    {
        for (var i = tokens.Count - 1; i >= 0; i--)
        {
            if (tokens[i].Kind == KqlTokenKind.Ident && session.TryGetField(tokens[i].Text, out var field))
                return field;
        }

        return null;
    }

    private static bool TryTokens(string text, out List<KqlToken> tokens)
    {
        tokens = [];
        if (text.Length == 0)
            return true;
        try
        {
            var lexer = new KqlLexer(text);
            while (true)
            {
                var token = lexer.Next();
                if (token.Kind == KqlTokenKind.Eof)
                    return true;
                tokens.Add(token);
            }
        }
        catch (KqlLexException)
        {
            return false;
        }
    }

    private static int PartialStart(string head)
    {
        if (head.Length == 0 || char.IsWhiteSpace(head[^1]))
            return head.Length;
        var i = head.Length - 1;
        while (i >= 0 && (char.IsLetterOrDigit(head[i]) || head[i] is '_' or '.'))
            i--;
        return i + 1;
    }

    private static bool InsideConstructor(string head)
    {
        var open = Math.Max(
            head.LastIndexOf("ipaddress(", StringComparison.OrdinalIgnoreCase),
            Math.Max(head.LastIndexOf("macaddress(", StringComparison.OrdinalIgnoreCase), head.LastIndexOf("string(", StringComparison.OrdinalIgnoreCase)));
        if (open < 0)
            return false;
        return head.IndexOf(')', open) < 0;
    }

    private static bool IsOperator(KqlTokenKind kind) => kind is KqlTokenKind.Eq or KqlTokenKind.Ne or KqlTokenKind.Lt or KqlTokenKind.Gt or KqlTokenKind.Le or KqlTokenKind.Ge or KqlTokenKind.Like or KqlTokenKind.NotLike or KqlTokenKind.Contains or KqlTokenKind.StartsWith or KqlTokenKind.EndsWith or KqlTokenKind.Between;

    private static bool IsPair(KqlToken token) => token.Kind == KqlTokenKind.Ident && token.Text.Equals("WITH", StringComparison.OrdinalIgnoreCase);

    private static bool IsValue(KqlTokenKind kind) => kind is KqlTokenKind.Number or KqlTokenKind.String or KqlTokenKind.True or KqlTokenKind.False or KqlTokenKind.IpAddress or KqlTokenKind.MacAddress or KqlTokenKind.StringValue or KqlTokenKind.TimeSpan;

    private static KqlCompletionRow Row(string insert, KqlCompletionKind kind) => new() { Insert = insert, Display = insert, Kind = kind };
}
