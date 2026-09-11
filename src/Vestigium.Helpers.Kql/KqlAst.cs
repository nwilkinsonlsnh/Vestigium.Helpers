namespace Vestigium.Helpers.Kql;

public abstract class KqlExpression
{
    public int Line { get; init; }
    public int Column { get; init; }
}

public sealed class KqlLogicalExpression : KqlExpression
{
    public required KqlExpression Left { get; init; }
    public required KqlLogicalOp Op { get; init; }
    public required KqlExpression Right { get; init; }
}

public sealed class KqlNotExpression : KqlExpression
{
    public required KqlExpression Operand { get; init; }
}

public sealed class KqlComparisonExpression : KqlExpression
{
    public required string Field { get; init; }
    public required KqlCompareOp Op { get; init; }
    public required KqlLiteral Value { get; init; }
    internal KqlField? BoundField { get; set; }
}

public enum KqlLogicalOp
{
    And = 0,
    Or = 1
}

public enum KqlCompareOp
{
    Eq = 0,
    Ne = 1,
    Lt = 2,
    Gt = 3,
    Le = 4,
    Ge = 5,
    Like = 6,
    NotLike = 7
}

public sealed class KqlLiteral
{
    public required KqlType Type { get; init; }
    public required object? Value { get; init; }
}

public sealed class KqlError
{
    public required int Line { get; init; }
    public required int Column { get; init; }
    public required string Message { get; init; }

    public override string ToString() => $"{Line}:{Column} {Message}";
}

public sealed class KqlParseResult
{
    public bool Ok => Error is null && Expression is not null;
    public KqlExpression? Expression { get; init; }
    public KqlError? Error { get; init; }

    public static KqlParseResult Success(KqlExpression expression)
        => new() { Expression = expression };

    public static KqlParseResult Fail(int line, int column, string message)
        => new() { Error = new KqlError { Line = line, Column = column, Message = message } };
}
