namespace Vestigium.Helpers.Pdf;

/// <summary>The report the PDF carries. Identity is the trailer.</summary>
public sealed class PdfReport
{
    public string JobId { get; init; } = "";

    public string Verb { get; init; } = "";

    public string Source { get; init; } = "";

    public string Destination { get; init; } = "";

    public string Algorithm { get; init; } = "";

    public int Matched { get; init; }

    public int Failed { get; init; }

    public IReadOnlyList<PdfRow> Rows { get; init; } = Array.Empty<PdfRow>();

    public PdfIdentity Identity { get; init; } = PdfIdentity.Capture();
}
