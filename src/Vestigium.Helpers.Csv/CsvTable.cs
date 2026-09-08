namespace Vestigium.Helpers.Csv;

/// <summary>
/// One rectangle. Modeled on ClosedXml <c>SheetTable</c> but a different CLR type —
/// this project does not reference ClosedXml.
/// </summary>
public sealed class CsvTable
{
    public required IReadOnlyList<string> Headers { get; init; }
    public required IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; }
    public string? Name { get; init; }

    public static CsvTable Create(
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<object?>> rows,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);
        if (headers.Count == 0)
            throw new ArgumentException("A table needs at least one header.", nameof(headers));

        return new CsvTable
        {
            Headers = headers,
            Rows = rows as IReadOnlyList<IReadOnlyList<object?>> ?? rows.ToArray(),
            Name = name
        };
    }

    public static CsvTable KeyValue(
        string keyHeader,
        string valueHeader,
        IEnumerable<(string Key, object? Value)> pairs,
        string? name = null)
        => Create(
            [keyHeader, valueHeader],
            pairs.Select(p => (IReadOnlyList<object?>)[p.Key, p.Value]),
            name);

    public static CsvTable Empty(string? name = null) => new()
    {
        Headers = [],
        Rows = [],
        Name = name
    };
}
