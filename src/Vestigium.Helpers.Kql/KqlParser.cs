namespace Vestigium.Helpers.Kql;

internal sealed class KqlParser
{
    private readonly KqlLexer _lexer;
    private KqlToken _current;

    private KqlParser(string text)
    {
        _lexer = new KqlLexer(text);
        _current = _lexer.Next();
    }

    public static KqlParseResult Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return KqlParseResult.Fail(1, 1, "query is empty");

        try
        {
            var parser = new KqlParser(text);
            var expr = parser.ParseOr();
            if (parser._current.Kind != KqlTokenKind.Eof)
            {
                return KqlParseResult.Fail(
                    parser._current.Line,
                    parser._current.Column,
                    $"unexpected '{parser._current.Text}'");
            }

            return KqlParseResult.Success(expr);
        }
        catch (KqlLexException ex)
        {
            return KqlParseResult.Fail(ex.Line, ex.Column, ex.Message);
        }
        catch (KqlParseException ex)
        {
            return KqlParseResult.Fail(ex.Line, ex.Column, ex.Message);
        }
    }

    private KqlExpression ParseOr()
    {
        var left = ParseAnd();
        while (_current.Kind == KqlTokenKind.Or)
        {
            var token = _current;
            Advance();
            left = new KqlLogicalExpression
            {
                Left = left,
                Op = KqlLogicalOp.Or,
                Right = ParseAnd(),
                Line = token.Line,
                Column = token.Column
            };
        }

        return left;
    }

    private KqlExpression ParseAnd()
    {
        var left = ParseNot();
        while (_current.Kind == KqlTokenKind.And)
        {
            var token = _current;
            Advance();
            left = new KqlLogicalExpression
            {
                Left = left,
                Op = KqlLogicalOp.And,
                Right = ParseNot(),
                Line = token.Line,
                Column = token.Column
            };
        }

        return left;
    }

    private KqlExpression ParseNot()
    {
        if (_current.Kind == KqlTokenKind.Not)
        {
            var token = _current;
            Advance();
            if (_current.Kind == KqlTokenKind.Like)
                throw new KqlParseException(token.Line, token.Column, "expected field name");

            return new KqlNotExpression
            {
                Operand = ParseNot(),
                Line = token.Line,
                Column = token.Column
            };
        }

        return ParsePrimary();
    }

    private KqlExpression ParsePrimary()
    {
        if (_current.Kind == KqlTokenKind.LParen)
        {
            Advance();
            var inner = ParseOr();
            if (_current.Kind != KqlTokenKind.RParen)
                throw Error(_current, "expected ')'");
            Advance();
            return inner;
        }

        return ParseComparison();
    }

    private KqlExpression ParseComparison()
    {
        if (_current.Kind != KqlTokenKind.Ident)
            throw Error(_current, "expected field name");

        var field = _current;
        Advance();
        if (KqlMethods.TrySplit(field.Text, out var owner, out var method))
            return ParseMethod(field, owner, method);
        if (_current.Kind == KqlTokenKind.LParen)
            return ParseClosedCall(field);
        if (IsLooseValue(_current))
            return ParseLoose(field);
        if (_current.Kind is KqlTokenKind.Eof or KqlTokenKind.And or KqlTokenKind.Or or KqlTokenKind.RParen)
            return BareTrue(field);

        switch (_current)
        {
            case { Kind: KqlTokenKind.In }:
                return ParseIn(field, negated: false);
            case { Kind: KqlTokenKind.Between }:
                return ParseBetween(field);
            case { Kind: KqlTokenKind.Not }:
            {
                var not = _current;
                Advance();
                switch (_current)
                {
                    case { Kind: KqlTokenKind.In }:
                        return ParseIn(field, negated: true);
                    case { Kind: KqlTokenKind.Like }:
                        Advance();
                        return new KqlComparisonExpression
                        {
                            Field = field.Text,
                            Op = KqlCompareOp.NotLike,
                            Value = ReadLiteral(),
                            Line = field.Line,
                            Column = field.Column
                        };
                    default:
                        throw new KqlParseException(not.Line, not.Column, "expected LIKE or IN after NOT");
                }
            }
        }

        var op = ReadCompareOp();
        var value = ReadLiteral();
        return new KqlComparisonExpression
        {
            Field = field.Text,
            Op = op,
            Value = value,
            Line = field.Line,
            Column = field.Column
        };
    }

    private KqlComparisonExpression ParseClosedCall(KqlToken field)
    {
        Advance();
        if (!IsValue(_current))
            throw Error(_current, "expected closed value");
        var token = _current;
        Advance();
        if (_current.Kind != KqlTokenKind.RParen)
            throw Error(_current, "expected ')'");
        Advance();
        var literal = new KqlLiteral { Type = KqlType.String, Value = token.Text, Form = KqlLiteralForm.Closed };
        return new KqlComparisonExpression
        {
            Field = field.Text,
            Op = KqlCompareOp.Eq,
            Value = literal,
            Line = field.Line,
            Column = field.Column
        };
    }

    private KqlInExpression ParseIn(KqlToken field, bool negated)
    {
        Advance();
        if (_current.Kind != KqlTokenKind.LParen)
            throw Error(_current, "expected '(' after IN");
        Advance();
        var values = new List<KqlLiteral> { ReadLiteral() };
        while (_current.Kind == KqlTokenKind.Comma)
        {
            Advance();
            values.Add(ReadLiteral());
        }

        if (_current.Kind != KqlTokenKind.RParen)
            throw Error(_current, "expected ')' after IN list");
        Advance();
        return new KqlInExpression
        {
            Field = field.Text,
            Values = values,
            Negated = negated,
            Line = field.Line,
            Column = field.Column
        };
    }

    private KqlBetweenExpression ParseBetween(KqlToken field)
    {
        Advance();
        var low = ReadLiteral();
        if (_current.Kind != KqlTokenKind.And)
            throw Error(_current, "expected AND in BETWEEN");
        Advance();
        var high = ReadLiteral();
        return new KqlBetweenExpression
        {
            Field = field.Text,
            Low = low,
            High = high,
            Line = field.Line,
            Column = field.Column
        };
    }

    private static bool IsValue(KqlToken token)
        => token.Kind is KqlTokenKind.Ident or KqlTokenKind.Number or KqlTokenKind.String or KqlTokenKind.StringValue
            or KqlTokenKind.True or KqlTokenKind.False or KqlTokenKind.DateValue or KqlTokenKind.TimeSpan
            or KqlTokenKind.IpAddress or KqlTokenKind.MacAddress
            or KqlTokenKind.And or KqlTokenKind.Or or KqlTokenKind.Not or KqlTokenKind.Like or KqlTokenKind.In
            or KqlTokenKind.Between or KqlTokenKind.Contains or KqlTokenKind.StartsWith or KqlTokenKind.EndsWith
            or KqlTokenKind.Gt or KqlTokenKind.Lt or KqlTokenKind.Ge or KqlTokenKind.Le or KqlTokenKind.Eq;

    private static bool IsLooseValue(KqlToken token)
        => token.Kind is KqlTokenKind.String or KqlTokenKind.StringValue or KqlTokenKind.Number
            or KqlTokenKind.True or KqlTokenKind.False or KqlTokenKind.Ident or KqlTokenKind.DateValue;

    private KqlExpression ParseLoose(KqlToken field)
    {
        var value = ReadLiteral();
        var boolean = value.Type == KqlType.Boolean || (value.Value is string text && KqlBooleans.TryParse(text, out _));
        var number = value.Type is KqlType.Integer or KqlType.Number;
        return new KqlComparisonExpression
        {
            Field = field.Text,
            Op = boolean || number ? KqlCompareOp.Eq : KqlCompareOp.Contains,
            Value = value,
            Line = field.Line,
            Column = field.Column
        };
    }

    private static KqlComparisonExpression BareTrue(KqlToken field)
        => new()
        {
            Field = field.Text,
            Op = KqlCompareOp.Eq,
            Value = new KqlLiteral { Type = KqlType.Boolean, Value = true },
            Line = field.Line,
            Column = field.Column
        };

    private KqlExpression ParseMethod(KqlToken token, string field, string method)
    {
        if (_current.Kind != KqlTokenKind.LParen)
            throw Error(_current, "expected '(' after method");
        Advance();
        var name = KqlMethods.Canonical(method) ?? method;
        if (KqlMethods.IsNullary(name))
        {
            if (_current.Kind != KqlTokenKind.RParen)
                throw Error(_current, "expected ')'");
            Advance();
            var value = name switch
            {
                "isTrue" => new KqlLiteral { Type = KqlType.Boolean, Value = true },
                "isFalse" => new KqlLiteral { Type = KqlType.Boolean, Value = false },
                _ => new KqlLiteral { Type = KqlType.String, Value = string.Empty }
            };
            return new KqlComparisonExpression
            {
                Field = field,
                Op = KqlMethods.ToOp(name),
                Value = value,
                Line = token.Line,
                Column = token.Column
            };
        }

        if (KqlMethods.IsBetween(name))
        {
            var low = ReadLiteral();
            if (_current.Kind == KqlTokenKind.Comma)
                Advance();
            var high = ReadLiteral();
            if (_current.Kind != KqlTokenKind.RParen)
                throw Error(_current, "expected ')'");
            Advance();
            return new KqlBetweenExpression
            {
                Field = field,
                Low = low,
                High = high,
                Line = token.Line,
                Column = token.Column
            };
        }

        var arg = ReadLiteral();
        if (_current.Kind != KqlTokenKind.RParen)
            throw Error(_current, "expected ')'");
        Advance();
        return new KqlComparisonExpression
        {
            Field = field,
            Op = KqlMethods.ToOp(name),
            Value = arg,
            Line = token.Line,
            Column = token.Column
        };
    }

    private KqlCompareOp ReadCompareOp()
    {
        if (_current.Kind == KqlTokenKind.Eq && _current.Text.Equals("IS", StringComparison.OrdinalIgnoreCase))
        {
            Advance();
            if (_current.Kind == KqlTokenKind.Not)
            {
                Advance();
                return KqlCompareOp.Ne;
            }

            return KqlCompareOp.Eq;
        }

        if (_current.Kind == KqlTokenKind.Not)
        {
            var token = _current;
            Advance();
            if (_current.Kind != KqlTokenKind.Like)
                throw new KqlParseException(token.Line, token.Column, "expected LIKE after NOT");
            Advance();
            return KqlCompareOp.NotLike;
        }

        if (_current.Kind == KqlTokenKind.Ident)
        {
            var word = _current.Text;
            if (word.Equals("BEGINS", StringComparison.OrdinalIgnoreCase) || word.Equals("ENDS", StringComparison.OrdinalIgnoreCase))
            {
                var begins = word.Equals("BEGINS", StringComparison.OrdinalIgnoreCase);
                Advance();
                if (_current.Kind != KqlTokenKind.Ident || !_current.Text.Equals("WITH", StringComparison.OrdinalIgnoreCase))
                    throw Error(_current, "expected WITH");
                Advance();
                return begins ? KqlCompareOp.BeginsWith : KqlCompareOp.EndsWith;
            }
        }

        var map = _current.Kind switch
        {
            KqlTokenKind.Eq => KqlCompareOp.Eq,
            KqlTokenKind.Ne => KqlCompareOp.Ne,
            KqlTokenKind.Lt => KqlCompareOp.Lt,
            KqlTokenKind.Gt => KqlCompareOp.Gt,
            KqlTokenKind.Le => KqlCompareOp.Le,
            KqlTokenKind.Ge => KqlCompareOp.Ge,
            KqlTokenKind.Like => KqlCompareOp.Like,
            KqlTokenKind.NotLike => KqlCompareOp.NotLike,
            KqlTokenKind.Contains => KqlCompareOp.Contains,
            KqlTokenKind.StartsWith => KqlCompareOp.BeginsWith,
            KqlTokenKind.EndsWith => KqlCompareOp.EndsWith,
            _ => (KqlCompareOp?)null
        };

        if (map is null)
            throw Error(_current, "expected comparison operator");

        Advance();
        return map.Value;
    }

    private KqlLiteral ReadLiteral()
    {
        var token = _current;
        switch (token)
        {
            case { Kind: KqlTokenKind.String }:
                Advance();
                return new KqlLiteral { Type = KqlType.String, Value = token.Text };
            case { Kind: KqlTokenKind.StringValue }:
                Advance();
                return new KqlLiteral { Type = KqlType.String, Value = token.Text, Form = KqlLiteralForm.String };
            case { Kind: KqlTokenKind.IpAddress }:
                Advance();
                return new KqlLiteral { Type = KqlType.String, Value = token.Text, Form = KqlLiteralForm.IpAddress };
            case { Kind: KqlTokenKind.MacAddress }:
                Advance();
                return new KqlLiteral { Type = KqlType.String, Value = token.Text, Form = KqlLiteralForm.MacAddress };
            case { Kind: KqlTokenKind.DateValue }:
                Advance();
                if (!KqlDates.TryParse(token.Text, out var date))
                    throw Error(token, "invalid date");
                return new KqlLiteral { Type = KqlType.DateTime, Value = date };
            case { Kind: KqlTokenKind.Ident }:
                Advance();
                if (_current.Kind != KqlTokenKind.LParen)
                    return new KqlLiteral { Type = KqlType.String, Value = token.Text, Form = KqlLiteralForm.Ident };
                Advance();
                if (_current.Kind is not (KqlTokenKind.Ident or KqlTokenKind.Number or KqlTokenKind.String))
                    throw Error(_current, "expected closed value");
                var inner = _current.Text;
                Advance();
                if (_current.Kind != KqlTokenKind.RParen)
                    throw Error(_current, "expected ')'");
                Advance();
                return new KqlLiteral { Type = KqlType.String, Value = token.Text + "." + inner, Form = KqlLiteralForm.Ident };
            case { Kind: KqlTokenKind.Number }:
                Advance();
                if (KqlUnits.TryParse(token.Text, out var scaled) && token.Text.Any(char.IsLetter))
                    return new KqlLiteral { Type = KqlType.Number, Value = scaled };
                if (token.Text.Contains('.')
                    && double.TryParse(token.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var real))
                    return new KqlLiteral { Type = KqlType.Number, Value = real };
                return long.TryParse(token.Text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var whole) ? new KqlLiteral { Type = KqlType.Integer, Value = whole } : throw Error(token, "invalid number");
            case { Kind: KqlTokenKind.True }:
                Advance();
                return new KqlLiteral { Type = KqlType.Boolean, Value = true };
            case { Kind: KqlTokenKind.False }:
                Advance();
                return new KqlLiteral { Type = KqlType.Boolean, Value = false };
            case { Kind: KqlTokenKind.And or KqlTokenKind.Or or KqlTokenKind.Not or KqlTokenKind.Like or KqlTokenKind.In or KqlTokenKind.Between or KqlTokenKind.Contains or KqlTokenKind.StartsWith or KqlTokenKind.EndsWith or KqlTokenKind.Gt or KqlTokenKind.Lt or KqlTokenKind.Ge or KqlTokenKind.Le or KqlTokenKind.Eq }:
                Advance();
                return new KqlLiteral { Type = KqlType.String, Value = token.Text };
            case { Kind: KqlTokenKind.TimeSpan }:
                Advance();
                return new KqlLiteral { Type = KqlType.TimeSpan, Value = ParseTimeSpan(token.Text) };
            case { Kind: KqlTokenKind.Eof }:
                throw new KqlParseException(token.Line, token.Column, "expected value");
            default:
                throw Error(token, "expected value");
        }
    }

    private static TimeSpan ParseTimeSpan(string text)
    {
        var i = 0;
        while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.'))
            i++;
        var number = double.Parse(text[..i], System.Globalization.CultureInfo.InvariantCulture);
        var unit = text[i..];
        return unit.ToLowerInvariant() switch
        {
            "ms" => TimeSpan.FromMilliseconds(number),
            "s" => TimeSpan.FromSeconds(number),
            "m" => TimeSpan.FromMinutes(number),
            "h" => TimeSpan.FromHours(number),
            "d" => TimeSpan.FromDays(number),
            _ => TimeSpan.FromSeconds(number)
        };
    }

    private void Advance() => _current = _lexer.Next();

    private static KqlParseException Error(KqlToken token, string message)
        => new(token.Line, token.Column, string.IsNullOrEmpty(token.Text) ? message : $"{message} at '{token.Text}'");
}

internal sealed class KqlParseException(int line, int column, string message) : Exception(message)
{
    public int Line { get; } = line;
    public int Column { get; } = column;
}
