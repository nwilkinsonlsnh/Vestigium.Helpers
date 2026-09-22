namespace Vestigium.Helpers.Encryption;

public sealed class EncryptionFileInfo
{
    public string SuiteVersion { get; init; } = "1.0";
    public string HeaderVersion { get; init; } = "1.0";
    public string TrailerVersion { get; init; } = "1.0";
    public EncryptionAlgorithm Algorithm { get; init; }
    public bool UsedArgon2id { get; init; }
    public int FrameSize { get; init; }
    public ulong FrameCount { get; init; }
    public ulong PlaintextLength { get; init; }
    public DateTimeOffset? CreatedUtc { get; init; }
    public bool Sha256ReservedFilled { get; init; }
    public bool HmacSha256ReservedFilled { get; init; }
    public string? PlaintextSha256Hex { get; init; }
    public string? CallerHmacHex { get; init; }
    public HmacCoverage HmacCoverage { get; init; }
    public bool HasHiddenOriginalName { get; init; }
    public string? OriginalFileName { get; init; }
    public bool HasRsaWrap { get; init; }
    public int WrapCount { get; init; }
    public IReadOnlyList<string> WrapThumbprints { get; init; } = [];
}
