using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Kql;

public sealed class KqlCompileResult
{
    public bool Ok => Error is null && Query is not null;
    public KqlBoundQuery? Query { get; init; }
    public KqlError? Error { get; init; }
    public IReadOnlyList<string> Diagnostics { get; init; } = [];

    public static KqlCompileResult Success(KqlBoundQuery query, IReadOnlyList<string>? diagnostics = null)
        => new() { Query = query, Diagnostics = diagnostics ?? [] };

    public static KqlCompileResult Fail(int line, int column, string message, IReadOnlyList<string>? diagnostics = null)
        => new() { Error = new KqlError { Line = line, Column = column, Message = message }, Diagnostics = diagnostics ?? [] };
}

public sealed class KqlBoundQuery
{
    internal KqlBoundQuery(KqlExpression expression, KqlSession session)
    {
        Expression = expression;
        Session = session;
    }

    public KqlExpression Expression { get; }
    public KqlSession Session { get; }

    public KqlTriState Evaluate(IKqlRow row) => KqlEvaluator.Evaluate(Expression, row);

    public bool Matches(IKqlRow row) => Evaluate(row) == KqlTriState.True;
}

internal static class KqlBinder
{
    public static KqlCompileResult Compile(string text, KqlSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var parsed = KqlParser.Parse(text);
        if (!parsed.Ok)
            return KqlCompileResult.Fail(parsed.Error!.Line, parsed.Error.Column, parsed.Error.Message);

        var diagnostics = new List<string>();
        try
        {
            Bind(parsed.Expression!, session, diagnostics);
            return KqlCompileResult.Success(new KqlBoundQuery(parsed.Expression!, session), diagnostics);
        }
        catch (KqlParseException ex)
        {
            HelperLog.Warning(
                HelperLog.AppIds.Kql,
                VestigiumStatus.Failed,
                HelperLog.Subcategories.Query,
                $"compile failed line={ex.Line} col={ex.Column}");
            return KqlCompileResult.Fail(ex.Line, ex.Column, ex.Message, diagnostics);
        }

        static void Bind(KqlExpression expr, KqlSession session, List<string> diagnostics)
        {
            while (true)
            {
                switch (expr)
                {
                    case KqlLogicalExpression logical:
                        Bind(logical.Left, session, diagnostics);
                        expr = logical.Right;
                        continue;
                    case KqlNotExpression not:
                        expr = not.Operand;
                        continue;
                    case KqlComparisonExpression cmp:
                    {
                        var field = RequireField(cmp.Field, cmp.Line, cmp.Column, session);
                        cmp.BoundField = field;
                        CheckRhs(field, cmp);
                        var like = cmp.Op is KqlCompareOp.Like or KqlCompareOp.NotLike or KqlCompareOp.BeginsWith or KqlCompareOp.EndsWith or KqlCompareOp.Contains;
                        CheckTypes(field, cmp.Op.ToString(), cmp.Value.Type, cmp.Line, cmp.Column, like);
                        WarnExactWildcard(cmp, field, diagnostics);
                        break;
                    }
                    case KqlInExpression inn:
                    {
                        var field = RequireField(inn.Field, inn.Line, inn.Column, session);
                        inn.BoundField = field;
                        if (inn.Values.Count == 0) throw new KqlParseException(inn.Line, inn.Column, "IN list is empty");
                        foreach (var value in inn.Values)
                        {
                            CheckLiteral(field, value, KqlCompareOp.Eq, inn.Line, inn.Column);
                            CheckTypes(field, "IN", value.Type, inn.Line, inn.Column, like: false);
                        }
                        break;
                    }
                    case KqlBetweenExpression between:
                    {
                        var field = RequireField(between.Field, between.Line, between.Column, session);
                        between.BoundField = field;
                        CheckTypes(field, "BETWEEN", between.Low.Type, between.Line, between.Column, like: false);
                        CheckTypes(field, "BETWEEN", between.High.Type, between.Line, between.Column, like: false);
                        CheckRange(field, between.Low, between.Line, between.Column);
                        CheckRange(field, between.High, between.Line, between.Column);
                        break;
                    }
                    default:
                        throw new KqlParseException(expr.Line, expr.Column, "unsupported expression");
                }

                break;
            }
        }
    }

    private static KqlField RequireField(string name, int line, int column, KqlSession session)
    {
        var dot = name.LastIndexOf('.');
        if (dot > 0 && name.IndexOf('.') != dot && session.TryGetField(name[..dot], out _))
            throw new KqlParseException(line, column, $"closed value '{name}' is not a field");
        return !session.TryGetField(name, out var field) ? throw new KqlParseException(line, column, UnknownFieldMessage(name, session)) : field;
    }

    private static void CheckRhs(KqlField field, KqlComparisonExpression cmp)
    {
        CheckLiteral(field, cmp.Value, cmp.Op, cmp.Line, cmp.Column);
        if (cmp.Op == KqlCompareOp.Eq)
            CheckRange(field, cmp.Value, cmp.Line, cmp.Column);
    }

    private static void CheckLiteral(KqlField field, KqlLiteral value, KqlCompareOp op, int line, int column)
    {
        switch (value.Form)
        {
            case KqlLiteralForm.Ident:
                ResolveIdent(field, value.Value as string ?? string.Empty, line, column);
                break;
            case KqlLiteralForm.Closed:
                RequireClosed(field, value.Value as string ?? string.Empty, line, column);
                break;
            case KqlLiteralForm.IpAddress:
                ValidateIp(value.Value as string ?? string.Empty, op, line, column);
                break;
            case KqlLiteralForm.MacAddress:
                ValidateMac(value.Value as string ?? string.Empty, op, line, column);
                break;
        }
    }

    private static void ResolveIdent(KqlField field, string text, int line, int column)
    {
        if (field.IsClosed(text))
            return;
        var dot = text.LastIndexOf('.');
        if (dot > 0 && text.IndexOf('.') != dot)
        {
            var owner = text[..dot];
            var tail = text[(dot + 1)..];
            if (!owner.Equals(field.Canonical, StringComparison.OrdinalIgnoreCase) || !field.IsClosed(tail))
                throw new KqlParseException(line, column, $"closed value '{text}' is not a value of {field.Canonical}");
            return;
        }

        throw new KqlParseException(line, column, $"'{text}' is not a closed value of {field.Canonical}");
    }

    private static void RequireClosed(KqlField field, string text, int line, int column)
    {
        if (!field.IsClosed(text))
            throw new KqlParseException(line, column, $"'{text}' is not a closed value of {field.Canonical}");
    }

    private static void ValidateIp(string text, KqlCompareOp op, int line, int column)
    {
        var parts = text.Split('.');
        if (parts.Length is 0 or > 4 || parts.Any(part => part.Length == 0))
            throw new KqlParseException(line, column, "empty or invalid ipaddress octet");
        foreach (var part in parts)
        {
            if (!int.TryParse(part, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var octet) || octet is < 0 or > 255)
                throw new KqlParseException(line, column, $"invalid ipaddress octet '{part}'");
        }

        if (op is KqlCompareOp.Eq or KqlCompareOp.Ne && parts.Length != 4)
            throw new KqlParseException(line, column, "== ipaddress requires four octets");
    }

    private static void ValidateMac(string text, KqlCompareOp op, int line, int column)
    {
        var hex = new string(text.Where(ch => ch is not ':' and not '-' and not ' ').ToArray());
        if (hex.Length == 0 || hex.Any(ch => !Uri.IsHexDigit(ch)))
            throw new KqlParseException(line, column, "macaddress is not hex");
        if (op is KqlCompareOp.Eq or KqlCompareOp.Ne)
        {
            if (hex.Length != 12)
                throw new KqlParseException(line, column, "== macaddress requires 12 hex digits");
            return;
        }

        if (hex.Length < 2 || hex.Length % 2 != 0)
            throw new KqlParseException(line, column, "macaddress fragment length must be even");
    }

    private static void CheckRange(KqlField field, KqlLiteral value, int line, int column)
    {
        if (field.Minimum is null || field.Maximum is null)
            return;
        if (value.Value is not long number && !long.TryParse(value.Value?.ToString(), out number))
            return;
        if (number < field.Minimum || number > field.Maximum)
            throw new KqlParseException(line, column, $"port {number} is outside {field.Minimum}-{field.Maximum}");
    }

    internal static string UnknownFieldMessage(string name, KqlSession session)
    {
        var packs = string.Join(',', session.Packs);
        var groups = session.Groups.ToString().Replace(" ", "", StringComparison.Ordinal);
        var names = session.Fields
            .SelectMany(f => f.Aliases.Prepend(f.Canonical))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        const int take = 12;
        var head = string.Join(", ", names.Take(take));
        var rest = names.Length > take ? $" +{names.Length - take}" : "";
        return $"unknown field '{name}' on pack={packs}. enabled ({groups}): {head}{rest}";
    }

    private static void CheckTypes(KqlField field, string op, KqlType rhs, int line, int column, bool like)
    {
        if (like)
        {
            if (field.Type != KqlType.String || rhs != KqlType.String)
                throw new KqlParseException(line, column, $"field={field.Canonical} type={field.Type} op={op} rhs={rhs}");
            return;
        }

        if (!TypesCompatible(field.Type, rhs))
        {
            throw new KqlParseException(
                line,
                column,
                $"field={field.Canonical} type={field.Type} op={op} rhs={rhs}");
        }
    }

    private static void WarnExactWildcard(KqlComparisonExpression cmp, KqlField field, List<string> diagnostics)
    {
        if (cmp.Op is not (KqlCompareOp.Eq or KqlCompareOp.Ne))
            return;
        if (cmp.Value.Form != KqlLiteralForm.Plain || cmp.Value.Type != KqlType.String || cmp.Value.Value is not string text)
            return;

        var chars = new System.Text.StringBuilder();
        if (text.Contains('%')) chars.Append('%');
        if (text.Contains('*')) chars.Append('*');
        if (text.Contains('?')) chars.Append('?');
        if (chars.Length == 0)
            return;

        var op = cmp.Op == KqlCompareOp.Eq ? "==" : "!=";
        var line = $"exact compare treats wildcard chars as literals field={field.Canonical} op={op} chars={chars}";
        diagnostics.Add(line);
        HelperLog.Warning(
            HelperLog.AppIds.Kql,
            VestigiumStatus.Warning,
            HelperLog.Subcategories.Query,
            line);
    }

    private static bool TypesCompatible(KqlType field, KqlType literal)
    {
        if (field == literal)
            return true;
        return field switch
        {
            KqlType.Integer or KqlType.Number when literal is KqlType.Integer or KqlType.Number => true,
            _ => false
        };
    }
}
