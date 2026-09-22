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
    }

    private static void Bind(KqlExpression expr, KqlSession session, List<string> diagnostics)
    {
        switch (expr)
        {
            case KqlLogicalExpression logical:
                Bind(logical.Left, session, diagnostics);
                Bind(logical.Right, session, diagnostics);
                break;
            case KqlNotExpression not:
                Bind(not.Operand, session, diagnostics);
                break;
            case KqlComparisonExpression cmp:
            {
                var field = RequireField(cmp.Field, cmp.Line, cmp.Column, session);
                cmp.BoundField = field;
                CheckTypes(field, cmp.Op.ToString(), cmp.Value.Type, cmp.Line, cmp.Column, like: cmp.Op is KqlCompareOp.Like or KqlCompareOp.NotLike);
                WarnExactWildcard(cmp, field, diagnostics);
                break;
            }
            case KqlInExpression inn:
            {
                var field = RequireField(inn.Field, inn.Line, inn.Column, session);
                inn.BoundField = field;
                if (inn.Values.Count == 0)
                    throw new KqlParseException(inn.Line, inn.Column, "IN list is empty");
                foreach (var value in inn.Values)
                    CheckTypes(field, "IN", value.Type, inn.Line, inn.Column, like: false);
                break;
            }
            case KqlBetweenExpression between:
            {
                var field = RequireField(between.Field, between.Line, between.Column, session);
                between.BoundField = field;
                CheckTypes(field, "BETWEEN", between.Low.Type, between.Line, between.Column, like: false);
                CheckTypes(field, "BETWEEN", between.High.Type, between.Line, between.Column, like: false);
                break;
            }
            default:
                throw new KqlParseException(expr.Line, expr.Column, "unsupported expression");
        }
    }

    private static KqlField RequireField(string name, int line, int column, KqlSession session)
    {
        if (!session.TryGetField(name, out var field))
            throw new KqlParseException(line, column, UnknownFieldMessage(name, session));
        return field;
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
        if (cmp.Value.Type != KqlType.String || cmp.Value.Value is not string text)
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
        if (field is KqlType.Integer or KqlType.Number && literal is KqlType.Integer or KqlType.Number)
            return true;
        return false;
    }
}
