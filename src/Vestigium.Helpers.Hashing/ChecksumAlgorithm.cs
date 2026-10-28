namespace Vestigium.Helpers.Hashing;

/// <summary>
/// Non-cryptographic checksums. Never the default of <see cref="HashingHelper.HashString"/>.
/// CRC-32 is IEEE / ISO-HDLC (PKZIP, PNG). CRC-64 is CRC-64/ECMA-182 (BCL <c>Crc64</c>, not XZ).
/// xxHash seed is 0. These do not authenticate; use HMAC-SHA256 or SHA-256 against an adversary.
/// </summary>
public enum ChecksumAlgorithm
{
    Crc32 = 1,
    Crc64 = 2,
    XxHash32 = 10,
    XxHash64 = 11,
    XxHash3 = 12,
}
