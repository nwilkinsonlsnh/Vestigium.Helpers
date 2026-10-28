namespace Vestigium.Helpers.Kql;

public enum KqlPrepareState
{
    Empty = 0,
    Partial = 1,
    Ready = 2,
    Failed = 3
}

public sealed class KqlPrepared
{
    public KqlPrepareState State { get; init; }

    public KqlBoundQuery? Query { get; init; }

    public string Message { get; init; } = string.Empty;

    public static KqlPrepared None(KqlPrepareState state) => new() { State = state };

    public static KqlPrepared Fail(string message) => new() { State = KqlPrepareState.Failed, Message = message };
}

public static class KqlPrepare
{
    public static KqlPrepared Run(string? text, KqlSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (string.IsNullOrWhiteSpace(text) || !HasCriteria(text, session))
            return KqlPrepared.None(string.IsNullOrWhiteSpace(text) ? KqlPrepareState.Empty : KqlPrepareState.Partial);

        var compiled = KqlHelper.Compile(Expand(text, session), session);
        return compiled.Ok
            ? new KqlPrepared { State = KqlPrepareState.Ready, Query = compiled.Query }
            : KqlPrepared.Fail(compiled.Error?.Message ?? "query failed");
    }

    private static bool HasCriteria(string text, KqlSession session)
    {
        var body = text.Trim();
        if (body.Length == 0)
            return false;
        var open = body.IndexOf('(');
        if (open >= 0)
            return body.IndexOf(')', open + 1) >= open + 1;
        if (body.IndexOfAny(['=', '!', '<', '>']) >= 0)
            return true;
        if (body.Contains(' ') && HasOperator(body))
            return true;
        var token = body.TrimEnd('.').Trim();
        if (token.Length == 0 || IsFieldPrefix(token, session))
            return false;
        return !body.Contains('.');
    }

    private static string Expand(string text, KqlSession session)
    {
        var body = text.Trim().Trim('"');
        if (body.Length == 0 || body.Contains('.') || HasOperator(body) || body.Contains('('))
            return text;
        var token = body.TrimEnd('.').Trim();
        if (IsFieldPrefix(token, session))
            return text;
        var fields = session.Fields.Where(field => field.Type == KqlType.String).Select(field => field.Canonical).ToArray();
        if (fields.Length == 0)
            return text;
        var value = body.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return string.Join(" || ", fields.Select(field => $"{field} CONTAINS \"{value}\""));
    }

    private static bool IsFieldPrefix(string token, KqlSession session)
        => session.Fields.Any(field =>
            field.Canonical.StartsWith(token, StringComparison.OrdinalIgnoreCase)
            || field.Aliases.Any(alias => alias.StartsWith(token, StringComparison.OrdinalIgnoreCase)));

    private static bool HasOperator(string text)
        => text.Contains("==", StringComparison.Ordinal)
            || text.Contains("!=", StringComparison.Ordinal)
            || text.Contains("<=", StringComparison.Ordinal)
            || text.Contains(">=", StringComparison.Ordinal)
            || text.Contains(" AND ", StringComparison.OrdinalIgnoreCase)
            || text.Contains(" OR ", StringComparison.OrdinalIgnoreCase)
            || text.Contains(" NOT ", StringComparison.OrdinalIgnoreCase)
            || text.Contains(" LIKE ", StringComparison.OrdinalIgnoreCase)
            || text.Contains(" CONTAINS ", StringComparison.OrdinalIgnoreCase)
            || text.Contains(" BETWEEN ", StringComparison.OrdinalIgnoreCase)
            || text.Contains(" IN ", StringComparison.OrdinalIgnoreCase)
            || text.Contains(" IS ", StringComparison.OrdinalIgnoreCase);
}
