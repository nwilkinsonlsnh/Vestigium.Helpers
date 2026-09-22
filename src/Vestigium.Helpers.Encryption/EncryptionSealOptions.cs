namespace Vestigium.Helpers.Encryption;

/// <summary>
/// Optional trailer integrity written at Seal time (Encryption v1.3 slot fill).
/// Does not replace the structural trailer <c>mac</c>. The caller MAC key is portable
/// and must not be the content key. Losing it does not prevent Open.
/// </summary>
public sealed class EncryptionSealOptions
{
    public const int MinCallerMacKeyLength = 16;

    public bool EmbedPlaintextSha256 { get; init; }

    /// <summary>Portable caller MAC key. Min 16 bytes. Not the content key. Not written to the envelope.</summary>
    public byte[]? CallerMacKey { get; init; }

    public HmacCoverage Coverage { get; init; }

    public bool WantsCallerMac =>
        CallerMacKey is { Length: > 0 } && Coverage is not HmacCoverage.None;

    public bool WantsAny => EmbedPlaintextSha256 || WantsCallerMac;
}
