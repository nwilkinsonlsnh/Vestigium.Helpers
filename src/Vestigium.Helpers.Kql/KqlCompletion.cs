namespace Vestigium.Helpers.Kql;

public enum KqlCompletionSlot
{
    None = 0,
    Field = 1,
    Operator = 2,
    Value = 3,
    Join = 4
}

public enum KqlCompletionKind
{
    Field = 0,
    Operator = 1,
    Value = 2,
    Constructor = 3,
    Join = 4
}

public sealed class KqlCompletionRow
{
    public required string Insert { get; init; }
    public required string Display { get; init; }
    public KqlCompletionKind Kind { get; init; }
}

public sealed class KqlCompletion
{
    public static KqlCompletion Empty { get; } = new();

    public KqlCompletionSlot Slot { get; init; }
    public IReadOnlyList<KqlCompletionRow> Rows { get; init; } = [];
    public int ReplaceStart { get; init; }
    public int ReplaceLength { get; init; }
}
