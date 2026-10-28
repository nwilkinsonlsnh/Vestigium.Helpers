namespace Vestigium.Helpers.Pdf;

/// <summary>A document the caller builds. Sections render in order. The trailer is opt-in.</summary>
public sealed class PdfDocument
{
    public string Title { get; init; } = "Vestigium";

    public string Subtitle { get; init; } = "";

    public string Banner { get; init; } = "#1B3A4B";

    public string BannerText { get; init; } = "#FFFFFF";

    public List<PdfSection> Sections { get; } = new();

    public bool Seal { get; init; }

    public PdfIdentity Identity { get; init; } = PdfIdentity.Capture();

    public PdfDocument AddFields(string heading, params PdfField[] fields)
    {
        Sections.Add(new PdfSection { Heading = heading, Fields = fields });
        return this;
    }

    public PdfDocument AddTable(string heading, string[] columns, IEnumerable<string[]> rows)
    {
        Sections.Add(new PdfSection { Heading = heading, Table = new PdfTable { Columns = columns, Rows = rows.ToArray() } });
        return this;
    }
}

public sealed class PdfSection
{
    public string Heading { get; init; } = "";

    public IReadOnlyList<PdfField>? Fields { get; init; }

    public PdfTable? Table { get; init; }
}

public sealed class PdfField
{
    public string Name { get; init; } = "";

    public string Value { get; init; } = "";
}

public sealed class PdfTable
{
    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string[]> Rows { get; init; } = Array.Empty<string[]>();
}
