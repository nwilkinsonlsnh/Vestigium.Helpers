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

    private KqlComparisonExpression ParseComparison()
    {
        if (_current.Kind != KqlTokenKind.Ident)
            throw Error(_current, "expected field name");

        var field = _current;
        Advance();

        var op = ReadCompareOp();
        var value = ReadLiteral();
        return new KqlComparisonExpression
        {
            Field = field.Text,
            Op = op.Op,
            Value = value,
            Line = field.Line,
            Column = field.Column
        };
    }

    private (KqlCompareOp Op, KqlToken Token) ReadCompareOp()
    {
        if (_current.Kind == KqlTokenKind.Not && PeekLike())
        {
            var token = _current;
            Advance();
            Advance();
            return (KqlCompareOp.NotLike, token);
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
            _ => (KqlCompareOp?)null
        };

        if (map is null)
            throw Error(_current, "expected comparison operator");

        var current = _current;
        Advance();
        return (map.Value, current);
    }

    private bool PeekLike()
    {
        // NOT LIKE is two tokens; lexer already consumed NOT as current.
        // We cannot peek the lexer easily; handle NOT LIKE in ParseComparison
        // by checking current is Not and next ident is LIKE after advance.
        return false;
    }

    private KqlLiteral ReadLiteral()
    {
        var token = _current;
        switch (token.Kind)
        {
            case KqlTokenKind.String:
                Advance();
                return new KqlLiteral { Type = KqlType.String, Value = token.Text };
            case KqlTokenKind.Number:
                Advance();
                if (token.Text.Contains('.') && double.TryParse(token.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var real))
                    return new KqlLiteral { Type = KqlType.Number, Value = real };
                if (long.TryParse(token.Text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var whole))
                    return new KqlLiteral { Type = KqlType.Integer, Value = whole };
                throw Error(token, "invalid number");
            case KqlTokenKind.True:
                Advance();
                return new KqlLiteral { Type = KqlType.Boolean, Value = true };
            case KqlTokenKind.False:
                Advance();
                return new KqlLiteral { Type = KqlType.Boolean, Value = false };
            case KqlTokenKind.TimeSpan:
                Advance();
                return new KqlLiteral { Type = KqlType.TimeSpan, Value = ParseTimeSpan(token.Text) };
            case KqlTokenKind.Eof:
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
