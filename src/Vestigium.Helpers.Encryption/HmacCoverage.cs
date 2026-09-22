namespace Vestigium.Helpers.Encryption;

/// <summary>
/// What the caller HMAC in the trailer <c>hmacSha256</c> slot covers.
/// Stored in reserved[0] when filled envelopes are written.
/// Hashing HMAC itself is always "these bytes + this key"; this enum is an Encryption Seal option.
/// </summary>
public enum HmacCoverage : byte
{
    /// <summary>No caller HMAC. Slot stays zeros.</summary>
    None = 0,

    /// <summary>A — on-disk ciphertext frames (AEAD cipher||tag, or CBC IV||cipher||frame-mac).</summary>
    CiphertextFrames = 1,

    /// <summary>B — written header (magic through frameCount) then the same frame bytes as A.</summary>
    HeaderAndFrames = 2,

    /// <summary>C — raw plaintext bytes. Needs decrypt (or hashing while Sealing) to verify.</summary>
    Plaintext = 3
}
