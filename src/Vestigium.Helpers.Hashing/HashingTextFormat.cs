namespace Vestigium.Helpers.Hashing;

/// <summary>
/// How to print a digest. Default is lowercase hex (FIPS / sha256sum).
/// Use <see cref="HashingConvert"/> to move between hex and Base64.
/// Password verifiers are PHC strings and ignore this enum.
/// </summary>
public enum HashingTextFormat
{
    HexLower = 0,
    HexUpper = 1,
    Base64 = 2,
    Base64Url = 3,
}
