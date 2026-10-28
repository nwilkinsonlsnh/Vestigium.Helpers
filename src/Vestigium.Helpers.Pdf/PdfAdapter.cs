namespace Vestigium.Helpers.Pdf;

/// <summary>One up adapter. Address is the first non-link-local unicast, IPv4 preferred.</summary>
public sealed class PdfAdapter
{
    public string Name { get; init; } = "";

    public string Address { get; init; } = "";

    public string Mac { get; init; } = "";
}
