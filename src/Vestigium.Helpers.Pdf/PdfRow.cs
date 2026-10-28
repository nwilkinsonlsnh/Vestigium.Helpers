namespace Vestigium.Helpers.Pdf;

/// <summary>One compared file.</summary>
public sealed class PdfRow
{
    public string Relative { get; init; } = "";

    public bool Equal { get; init; }

    public string? SourceDigest { get; init; }

    public string? DestinationDigest { get; init; }
}
