namespace Vestigium.Helpers.Kql;

internal enum KqlTokenKind
{
    Ident,
    Number,
    String,
    TimeSpan,
    True,
    False,
    Eq,
    Ne,
    Lt,
    Gt,
    Le,
    Ge,
    Like,
    NotLike,
    In,
    Between,
    And,
    Or,
    Not,
    LParen,
    RParen,
    Comma,
    Eof
}

internal readonly struct KqlToken
{
    public KqlToken(KqlTokenKind kind, string text, int line, int column)
    {
        Kind = kind;
        Text = text;
        Line = line;
        Column = column;
    }

    public KqlTokenKind Kind { get; }
    public string Text { get; }
    public int Line { get; }
    public int Column { get; }
}

internal sealed class KqlLexer
{
    private readonly string _text;
    private int _index;
    private int _line = 1;
    private int _column = 1;

    public KqlLexer(string text) => _text = text ?? string.Empty;

    public KqlToken Next()
    {
        SkipWhite();
        if (_index >= _text.Length)
            return new KqlToken(KqlTokenKind.Eof, string.Empty, _line, _column);

        var line = _line;
        var column = _column;
        var ch = _text[_index];

        if (ch == '|')
        {
            if (Peek(1) == '|')
            {
                Advance();
                Advance();
                return new KqlToken(KqlTokenKind.Or, "||", line, column);
            }

            throw new KqlLexException(line, column, "pipe is not supported");
        }

        if (ch == '&')
        {
            if (Peek(1) != '&')
                throw new KqlLexException(line, column, "unexpected '&'");
            Advance();
            Advance();
            return new KqlToken(KqlTokenKind.And, "&&", line, column);
        }

        if (ch == '(')
        {
            Advance();
            return new KqlToken(KqlTokenKind.LParen, "(", line, column);
        }

        if (ch == ')')
        {
            Advance();
            return new KqlToken(KqlTokenKind.RParen, ")", line, column);
        }

        if (ch == ',')
        {
            Advance();
            return new KqlToken(KqlTokenKind.Comma, ",", line, column);
        }

        if (ch == Quote || ch == DoubleQuote)
            return ReadString(ch, line, column);

        if (ch == '=' && Peek(1) == '=')
        {
            Advance();
            Advance();
            return new KqlToken(KqlTokenKind.Eq, "==", line, column);
        }

        if (ch == '!' && Peek(1) == '=')
        {
            Advance();
            Advance();
            return new KqlToken(KqlTokenKind.Ne, "!=", line, column);
        }

        if (ch == '<' && Peek(1) == '>')
        {
            Advance();
            Advance();
            return new KqlToken(KqlTokenKind.Ne, "<>", line, column);
        }

        if (ch == '<' && Peek(1) == '=')
        {
            Advance();
            Advance();
            return new KqlToken(KqlTokenKind.Le, "<=", line, column);
        }

        if (ch == '>' && Peek(1) == '=')
        {
            Advance();
            Advance();
            return new KqlToken(KqlTokenKind.Ge, ">=", line, column);
        }

        if (ch == '<')
        {
            Advance();
            return new KqlToken(KqlTokenKind.Lt, "<", line, column);
        }

        if (ch == '>')
        {
            Advance();
            return new KqlToken(KqlTokenKind.Gt, ">", line, column);
        }

        if (ch == '!')
        {
            Advance();
            SkipWhite();
            if (IsIdentStart(Peek(0)))
            {
                var word = ReadIdent();
                if (word.Equals("LIKE", StringComparison.OrdinalIgnoreCase))
                    return new KqlToken(KqlTokenKind.NotLike, "!LIKE", line, column);
            }

            throw new KqlLexException(line, column, "unexpected '!'");
        }

        if (char.IsDigit(ch) || (ch == '.' && char.IsDigit(Peek(1))))
            return ReadNumberOrTimeSpan(line, column);

        if (IsIdentStart(ch))
        {
            var ident = ReadIdent();
            while (Peek(0) == '.')
            {
                Advance();
                if (!IsIdentStart(Peek(0)))
                    throw new KqlLexException(_line, _column, "expected identifier after '.'");
                ident += "." + ReadIdent();
            }

            return Keyword(ident, line, column);
        }

        throw new KqlLexException(line, column, $"unexpected '{ch}'");
    }

    private const char Quote = (char)39;
    private const char DoubleQuote = (char)34;
    private const char Backslash = (char)92;

    private KqlToken Keyword(string ident, int line, int column)
    {
        if (ident.Equals("AND", StringComparison.OrdinalIgnoreCase))
            return new KqlToken(KqlTokenKind.And, ident, line, column);
        if (ident.Equals("OR", StringComparison.OrdinalIgnoreCase))
            return new KqlToken(KqlTokenKind.Or, ident, line, column);
        if (ident.Equals("NOT", StringComparison.OrdinalIgnoreCase))
            return new KqlToken(KqlTokenKind.Not, ident, line, column);
        if (ident.Equals("LIKE", StringComparison.OrdinalIgnoreCase))
            return new KqlToken(KqlTokenKind.Like, ident, line, column);
        if (ident.Equals("IN", StringComparison.OrdinalIgnoreCase))
            return new KqlToken(KqlTokenKind.In, ident, line, column);
        if (ident.Equals("BETWEEN", StringComparison.OrdinalIgnoreCase))
            return new KqlToken(KqlTokenKind.Between, ident, line, column);
        if (ident.Equals("GT", StringComparison.OrdinalIgnoreCase))
            return new KqlToken(KqlTokenKind.Gt, ident, line, column);
        if (ident.Equals("LT", StringComparison.OrdinalIgnoreCase))
            return new KqlToken(KqlTokenKind.Lt, ident, line, column);
        if (ident.Equals("GE", StringComparison.OrdinalIgnoreCase))
            return new KqlToken(KqlTokenKind.Ge, ident, line, column);
        if (ident.Equals("LE", StringComparison.OrdinalIgnoreCase))
            return new KqlToken(KqlTokenKind.Le, ident, line, column);
        if (ident.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            return new KqlToken(KqlTokenKind.True, ident, line, column);
        if (ident.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            return new KqlToken(KqlTokenKind.False, ident, line, column);
        return new KqlToken(KqlTokenKind.Ident, ident, line, column);
    }

    private KqlToken ReadString(char quote, int line, int column)
    {
        Advance();
        var buffer = new System.Text.StringBuilder();
        while (_index < _text.Length)
        {
            var ch = _text[_index];
            if (ch == quote)
            {
                Advance();
                return new KqlToken(KqlTokenKind.String, buffer.ToString(), line, column);
            }

            if (ch == Backslash)
            {
                Advance();
                if (_index >= _text.Length)
                    throw new KqlLexException(line, column, "unterminated string");
                var esc = _text[_index];
                buffer.Append(esc switch
                {
                    var q when q == Quote => Quote,
                    var q when q == DoubleQuote => DoubleQuote,
                    var q when q == Backslash => Backslash,
                    'n' => '\n',
                    't' => '\t',
                    _ => esc
                });
                Advance();
                continue;
            }

            if (ch == '\n')
                throw new KqlLexException(line, column, "unterminated string");
            buffer.Append(ch);
            Advance();
        }

        throw new KqlLexException(line, column, "unterminated string");
    }

    private KqlToken ReadNumberOrTimeSpan(int line, int column)
    {
        var start = _index;
        while (_index < _text.Length && (char.IsDigit(_text[_index]) || _text[_index] == '.'))
            Advance();

        var number = _text[start.._index];
        if (_index < _text.Length && char.IsLetter(_text[_index]))
        {
            var unitStart = _index;
            while (_index < _text.Length && char.IsLetter(_text[_index]))
                Advance();
            var unit = _text[unitStart.._index];
            if (unit.Equals("ms", StringComparison.OrdinalIgnoreCase)
                || unit.Equals("s", StringComparison.OrdinalIgnoreCase)
                || unit.Equals("m", StringComparison.OrdinalIgnoreCase)
                || unit.Equals("h", StringComparison.OrdinalIgnoreCase)
                || unit.Equals("d", StringComparison.OrdinalIgnoreCase))
                return new KqlToken(KqlTokenKind.TimeSpan, _text[start.._index], line, column);

            throw new KqlLexException(line, column, $"unexpected number suffix '{unit}'");
        }

        return new KqlToken(KqlTokenKind.Number, number, line, column);
    }

    private string ReadIdent()
    {
        var start = _index;
        Advance();
        while (_index < _text.Length && IsIdentPart(_text[_index]))
            Advance();
        return _text[start.._index];
    }

    private void SkipWhite()
    {
        while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
            Advance();
    }

    private char Peek(int ahead)
    {
        var i = _index + ahead;
        return i < _text.Length ? _text[i] : '\0';
    }

    private void Advance()
    {
        if (_index >= _text.Length)
            return;
        if (_text[_index] == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        _index++;
    }

    private static bool IsIdentStart(char ch) => char.IsLetter(ch) || ch == '_';

    private static bool IsIdentPart(char ch) => char.IsLetterOrDigit(ch) || ch == '_';
}

internal sealed class KqlLexException(int line, int column, string message) : Exception(message)
{
    public int Line { get; } = line;
    public int Column { get; } = column;
}
