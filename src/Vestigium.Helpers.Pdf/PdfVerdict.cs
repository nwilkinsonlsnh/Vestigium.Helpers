namespace Vestigium.Helpers.Pdf;

public enum PdfVerifyResult
{
    Valid,
    Tampered,
    NoTrailer,
}

/// <summary>What Verify found. It does not throw for a bad file.</summary>
public sealed class PdfVerdict
{
    public PdfVerifyResult Result { get; init; }

    public bool BodyMatches { get; init; }

    public bool SealChecked { get; init; }

    public bool SealMatches { get; init; }

    public string? BodyHash { get; init; }
}
