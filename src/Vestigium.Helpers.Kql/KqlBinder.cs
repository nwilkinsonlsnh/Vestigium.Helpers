using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Kql;

public sealed class KqlCompileResult
{
    public bool Ok => Error is null && Query is not null;
    public KqlBoundQuery? Query { get; init; }
    public KqlError? Error { get; init; }

    public static KqlCompileResult Success(KqlBoundQuery query) => new() { Query = query };

    public static KqlCompileResult Fail(int line, int column, string message)
        => new() { Error = new KqlError { Line = line, Column = column, Message = message } };
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

        try
        {
            Bind(parsed.Expression!, session);
            return KqlCompileResult.Success(new KqlBoundQuery(parsed.Expression!, session));
        }
        catch (KqlParseException ex)
        {
            HelperLog.Warning(
                HelperLog.AppIds.Kql,
                VestigiumStatus.Failed,
                HelperLog.Subcategories.Query,
                $"compile failed line={ex.Line} col={ex.Column}");
            return KqlCompileResult.Fail(ex.Line, ex.Column, ex.Message);
        }
    }

    private static void Bind(KqlExpression expr, KqlSession session)
    {
        switch (expr)
        {
            case KqlLogicalExpression logical:
                Bind(logical.Left, session);
                Bind(logical.Right, session);
                break;
            case KqlNotExpression not:
                Bind(not.Operand, session);
                break;
            case KqlComparisonExpression cmp:
                if (!session.TryGetField(cmp.Field, out var field))
                {
                    throw new KqlParseException(
                        cmp.Line,
                        cmp.Column,
                        UnknownFieldMessage(cmp.Field, session));
                }

                cmp.BoundField = field;
                CheckTypes(cmp, field);
                WarnExactWildcard(cmp, field);
                break;
            default:
                throw new KqlParseException(expr.Line, expr.Column, "unsupported expression");
        }
    }

    internal static string UnknownFieldMessage(string name, KqlSession session)
    {
        var packs = string.Join(',', session.Packs);
        var groups = session.Groups.ToString().Replace(" ", "", StringComparison.Ordinal);
        var names = session.Fields.Select(f => f.Canonical).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        const int take = 12;
        var head = string.Join(", ", names.Take(take));
        var rest = names.Length > take ? $" +{names.Length - take}" : "";
        return $"unknown field '{name}' on pack={packs}. enabled ({groups}): {head}{rest}";
    }

    private static void CheckTypes(KqlComparisonExpression cmp, KqlField field)
    {
        if (cmp.Op is KqlCompareOp.Like or KqlCompareOp.NotLike)
        {
            if (field.Type != KqlType.String || cmp.Value.Type != KqlType.String)
                throw new KqlParseException(cmp.Line, cmp.Column, $"LIKE requires string field and string value ({field.Canonical})");
            return;
        }

        if (!TypesCompatible(field.Type, cmp.Value.Type))
        {
            throw new KqlParseException(
                cmp.Line,
                cmp.Column,
                $"type mismatch {field.Canonical}:{field.Type} vs {cmp.Value.Type}");
        }
    }

    private static void WarnExactWildcard(KqlComparisonExpression cmp, KqlField field)
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
        HelperLog.Warning(
            HelperLog.AppIds.Kql,
            VestigiumStatus.Warning,
            HelperLog.Subcategories.Query,
            $"exact compare treats wildcard chars as literals field={field.Canonical} op={op} chars={chars}");
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
