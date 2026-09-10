namespace Vestigium.Helpers.Network;

public enum DataUnit
{
    Bit = 0,
    Byte = 1,
    Kb = 2,
    KB = 3,
    Kib = 4,
    KiB = 5,
    Mb = 6,
    MB = 7,
    Mib = 8,
    MiB = 9,
    Gb = 10,
    GB = 11,
    Gib = 12,
    GiB = 13,
    Tb = 14,
    TB = 15,
    Tib = 16,
    TiB = 17
}

public enum BandwidthBasis
{
    Day = 0,
    Days30 = 1,
    Year365 = 2
}

public enum CommonBotId
{
    Googlebot,
    GooglebotImage,
    GooglebotVideo,
    AdsBotGoogle,
    Bingbot,
    DuckDuckBot,
    Applebot,
    Amazonbot,
    YandexBot,
    Baiduspider,
    Slurp,
    FacebookBot,
    LinkedInBot,
    TwitterBot,
    AhrefsBot,
    SemrushBot,
    DotBot,
    PetalBot,
    GPTBot,
    ChatGPTUser,
    ClaudeBot,
    Bytespider,
    Other
}

public sealed record CommonBot(CommonBotId Id, string Display, string Operator);

public sealed record BandwidthAmount(decimal Bits, DataUnit DisplayUnit, string Display)
{
    public decimal Bytes => Bits / 8m;
}

public sealed record TransferResult(
    BandwidthAmount Size,
    BandwidthAmount Rate,
    TimeSpan Duration,
    string Summary);

public sealed record PeriodVolume(
    BandwidthBasis Basis,
    int Seconds,
    BandwidthAmount Volume,
    BandwidthAmount AverageRate,
    string Summary);

public sealed record BotHitRow(CommonBotId BotId, long Hits, string? OtherName = null);

public sealed class WebsiteTrafficQuery
{
    public BandwidthAmount? PageSize { get; set; }
    public long HumanHits { get; set; }
    public IReadOnlyList<BotHitRow> BotRows { get; set; } = [];
    public decimal AssetFactor { get; set; } = 1m;
    public decimal OriginRatio { get; set; } = 1m;
    public decimal PeakFactor { get; set; } = 1m;
    public BandwidthBasis Basis { get; set; } = BandwidthBasis.Days30;
}

public sealed record WebsiteTrafficResult(
    long HumanHits,
    long BotHits,
    long TotalHits,
    BandwidthAmount PageSize,
    BandwidthAmount TotalBytes,
    BandwidthAmount AverageRate,
    BandwidthAmount PeakRate,
    BandwidthBasis Basis,
    int Seconds,
    string Summary);
