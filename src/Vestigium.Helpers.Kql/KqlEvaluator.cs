namespace Vestigium.Helpers.Kql;

internal static class KqlEvaluator
{
    public static KqlTriState Evaluate(KqlExpression expr, IKqlRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return expr switch
        {
            KqlLogicalExpression logical => Combine(logical.Op, Evaluate(logical.Left, row), Evaluate(logical.Right, row)),
            KqlNotExpression not => Not(Evaluate(not.Operand, row)),
            KqlComparisonExpression cmp => Compare(cmp, row),
            KqlInExpression inn => In(inn, row),
            KqlBetweenExpression between => Between(between, row),
            _ => KqlTriState.Unknown
        };
    }

    private static KqlTriState Combine(KqlLogicalOp op, KqlTriState left, KqlTriState right)
    {
        if (op == KqlLogicalOp.And)
        {
            if (left == KqlTriState.False || right == KqlTriState.False)
                return KqlTriState.False;
            if (left == KqlTriState.Unknown || right == KqlTriState.Unknown)
                return KqlTriState.Unknown;
            return KqlTriState.True;
        }

        if (left == KqlTriState.True || right == KqlTriState.True)
            return KqlTriState.True;
        if (left == KqlTriState.Unknown || right == KqlTriState.Unknown)
            return KqlTriState.Unknown;
        return KqlTriState.False;
    }

    private static KqlTriState Not(KqlTriState value) => value switch
    {
        KqlTriState.True => KqlTriState.False,
        KqlTriState.False => KqlTriState.True,
        _ => KqlTriState.Unknown
    };

    private static KqlTriState Compare(KqlComparisonExpression cmp, IKqlRow row)
    {
        var field = cmp.BoundField ?? throw new InvalidOperationException("comparison is not bound");
        var left = row.Get(field.Canonical);
        if (left.IsUnknown)
            return KqlTriState.Unknown;

        var right = cmp.Value;
        if (cmp.Op is KqlCompareOp.Like or KqlCompareOp.NotLike)
        {
            var text = Convert.ToString(left.Raw, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            var pattern = Convert.ToString(right.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            var like = KqlLike.IsMatch(text, pattern);
            var hit = cmp.Op == KqlCompareOp.Like ? like : !like;
            return hit ? KqlTriState.True : KqlTriState.False;
        }

        var relation = CompareValues(left, right);
        if (relation is null)
            return KqlTriState.Unknown;

        var ok = cmp.Op switch
        {
            KqlCompareOp.Eq => relation == 0,
            KqlCompareOp.Ne => relation != 0,
            KqlCompareOp.Lt => relation < 0,
            KqlCompareOp.Gt => relation > 0,
            KqlCompareOp.Le => relation <= 0,
            KqlCompareOp.Ge => relation >= 0,
            _ => false
        };
        return ok ? KqlTriState.True : KqlTriState.False;
    }

    private static KqlTriState In(KqlInExpression inn, IKqlRow row)
    {
        var field = inn.BoundField ?? throw new InvalidOperationException("IN is not bound");
        var left = row.Get(field.Canonical);
        if (left.IsUnknown)
            return KqlTriState.Unknown;

        var hit = false;
        var anyUnknown = false;
        foreach (var value in inn.Values)
        {
            var relation = CompareValues(left, value);
            if (relation is null)
            {
                anyUnknown = true;
                continue;
            }

            if (relation == 0)
            {
                hit = true;
                break;
            }
        }

        if (hit)
            return inn.Negated ? KqlTriState.False : KqlTriState.True;
        if (anyUnknown)
            return KqlTriState.Unknown;
        return inn.Negated ? KqlTriState.True : KqlTriState.False;
    }

    private static KqlTriState Between(KqlBetweenExpression between, IKqlRow row)
    {
        var field = between.BoundField ?? throw new InvalidOperationException("BETWEEN is not bound");
        var left = row.Get(field.Canonical);
        if (left.IsUnknown)
            return KqlTriState.Unknown;

        var low = CompareValues(left, between.Low);
        var high = CompareValues(left, between.High);
        if (low is null || high is null)
            return KqlTriState.Unknown;
        return low >= 0 && high <= 0 ? KqlTriState.True : KqlTriState.False;
    }

    private static int? CompareValues(KqlValue left, KqlLiteral right)
    {
        if (left.Type is KqlType.Integer or KqlType.Number || right.Type is KqlType.Integer or KqlType.Number)
        {
            if (!TryNumber(left.Raw, out var lv) || !TryNumber(right.Value, out var rv))
                return null;
            return lv.CompareTo(rv);
        }

        if (left.Type == KqlType.String || right.Type == KqlType.String)
        {
            var ls = Convert.ToString(left.Raw, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            var rs = Convert.ToString(right.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            return string.Compare(ls, rs, StringComparison.OrdinalIgnoreCase);
        }

        if (left.Type == KqlType.Boolean && right.Value is bool rb && left.Raw is bool lb)
            return lb.CompareTo(rb);

        if (left.Type == KqlType.TimeSpan && left.Raw is TimeSpan lt && right.Value is TimeSpan rt)
            return lt.CompareTo(rt);

        return Comparer<object>.Default.Compare(left.Raw, right.Value);
    }

    private static bool TryNumber(object? value, out double number)
    {
        switch (value)
        {
            case null:
                number = 0;
                return false;
            case IConvertible:
                try
                {
                    number = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                }
                catch
                {
                    number = 0;
                    return false;
                }
            default:
                number = 0;
                return false;
        }
    }
}
