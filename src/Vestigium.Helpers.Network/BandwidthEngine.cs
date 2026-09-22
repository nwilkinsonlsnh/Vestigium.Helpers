using System.Globalization;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class BandwidthEngine
{
    public static readonly IReadOnlyList<CommonBot> Catalog =
    [
        new(CommonBotId.Googlebot, "Googlebot", "Google"),
        new(CommonBotId.GooglebotImage, "Googlebot-Image", "Google"),
        new(CommonBotId.GooglebotVideo, "Googlebot-Video", "Google"),
        new(CommonBotId.AdsBotGoogle, "AdsBot-Google", "Google"),
        new(CommonBotId.Bingbot, "Bingbot", "Microsoft"),
        new(CommonBotId.DuckDuckBot, "DuckDuckBot", "DuckDuckGo"),
        new(CommonBotId.Applebot, "Applebot", "Apple"),
        new(CommonBotId.Amazonbot, "Amazonbot", "Amazon"),
        new(CommonBotId.YandexBot, "YandexBot", "Yandex"),
        new(CommonBotId.Baiduspider, "Baiduspider", "Baidu"),
        new(CommonBotId.Slurp, "Slurp", "Yahoo"),
        new(CommonBotId.FacebookBot, "FacebookBot", "Meta"),
        new(CommonBotId.LinkedInBot, "LinkedInBot", "LinkedIn"),
        new(CommonBotId.TwitterBot, "Twitterbot", "X"),
        new(CommonBotId.AhrefsBot, "AhrefsBot", "Ahrefs"),
        new(CommonBotId.SemrushBot, "SemrushBot", "Semrush"),
        new(CommonBotId.DotBot, "DotBot", "Moz"),
        new(CommonBotId.PetalBot, "PetalBot", "Huawei"),
        new(CommonBotId.GPTBot, "GPTBot", "OpenAI"),
        new(CommonBotId.ChatGPTUser, "ChatGPT-User", "OpenAI"),
        new(CommonBotId.ClaudeBot, "ClaudeBot", "Anthropic"),
        new(CommonBotId.Bytespider, "Bytespider", "ByteDance"),
        new(CommonBotId.Other, "Other", "Operator")
    ];

    public static BandwidthAmount From(decimal value, DataUnit unit)
    {
        if (value < 0)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, "Bandwidth", nameof(From), $"value={value}");
            throw new ArgumentOutOfRangeException(nameof(value), "Amount cannot be negative.");
        }

        var bits = value * BitsPerUnit(unit);
        return new BandwidthAmount(bits, unit, Format(bits, unit));
    }

    public static BandwidthAmount Convert(BandwidthAmount amount, DataUnit unit)
        => new(amount.Bits, unit, Format(amount.Bits, unit));

    public static TransferResult TransferTime(BandwidthAmount size, BandwidthAmount rate)
    {
        if (rate.Bits <= 0 && size.Bits > 0)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, "Bandwidth", nameof(TransferTime), "rate=0");
            throw new InvalidOperationException("Rate is 0; transfer time is undefined.");
        }

        var seconds = size.Bits == 0 ? 0m : size.Bits / rate.Bits;
        var duration = TimeSpan.FromTicks((long)decimal.Round(seconds * TimeSpan.TicksPerSecond, 0, MidpointRounding.AwayFromZero));
        return new TransferResult(size, rate, duration, $"{size.Display} at {rate.Display}/s → {duration}");
    }

    public static BandwidthAmount RequiredRate(BandwidthAmount size, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero && size.Bits > 0)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, "Bandwidth", nameof(RequiredRate), "duration=0");
            throw new InvalidOperationException("Duration is 0; required rate is undefined.");
        }

        var seconds = (decimal)duration.TotalSeconds;
        var bitsPerSecond = seconds == 0 ? 0m : size.Bits / seconds;
        return new BandwidthAmount(bitsPerSecond, DataUnit.Mb, Format(bitsPerSecond, DataUnit.Mb) + "/s");
    }

    public static BandwidthAmount Transferred(BandwidthAmount rate, TimeSpan duration)
    {
        var bits = rate.Bits * (decimal)duration.TotalSeconds;
        return new BandwidthAmount(bits, DataUnit.MB, Format(bits, DataUnit.MB));
    }

    public static PeriodVolume VolumeFromRate(BandwidthAmount rate, BandwidthBasis basis)
    {
        var seconds = Seconds(basis);
        var bits = rate.Bits * seconds;
        var volume = new BandwidthAmount(bits, DataUnit.GB, Format(bits, DataUnit.GB));
        return new PeriodVolume(basis, seconds, volume, Convert(rate, DataUnit.Mb), $"{rate.Display}/s × {Label(basis)} ({seconds:N0} s) = {volume.Display}");
    }

    public static PeriodVolume RateFromVolume(BandwidthAmount volume, BandwidthBasis basis)
    {
        var seconds = Seconds(basis);
        var bitsPerSecond = volume.Bits / seconds;
        var rate = new BandwidthAmount(bitsPerSecond, DataUnit.Mb, Format(bitsPerSecond, DataUnit.Mb) + "/s");
        return new PeriodVolume(basis, seconds, volume, rate, $"{volume.Display} over {Label(basis)} ({seconds:N0} s) = {rate.Display}");
    }

    public static WebsiteTrafficResult EstimateWebsite(WebsiteTrafficQuery query)
    {
        var q = query ?? throw new ArgumentNullException(nameof(query));
        var page = q.PageSize ?? From(1, DataUnit.MB);
        if (q.HumanHits < 0 || q.AssetFactor < 0 || q.OriginRatio < 0 || q.PeakFactor < 0)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, "Bandwidth", nameof(EstimateWebsite), "negative input");
            throw new ArgumentOutOfRangeException(nameof(query), "Hits and factors cannot be negative.");
        }

        long botHits = 0;
        if (q.BotRows is not null)
        {
            foreach (var row in q.BotRows)
            {
                if (row.Hits < 0)
                    throw new ArgumentOutOfRangeException(nameof(query), "Bot hits cannot be negative.");
                botHits += row.Hits;
            }
        }

        var totalHits = q.HumanHits + botHits;
        var bits = page.Bits * totalHits * q.AssetFactor * q.OriginRatio;
        var seconds = Seconds(q.Basis);
        var avgBits = bits / seconds;
        var peakBits = avgBits * (q.PeakFactor <= 0 ? 1m : q.PeakFactor);
        var total = new BandwidthAmount(bits, DataUnit.GB, Format(bits, DataUnit.GB));
        var avg = new BandwidthAmount(avgBits, DataUnit.Mb, Format(avgBits, DataUnit.Mb) + "/s");
        var peak = new BandwidthAmount(peakBits, DataUnit.Mb, Format(peakBits, DataUnit.Mb) + "/s");
        NetworkLog.Success("Bandwidth", $"website hits={totalHits} bots={botHits} basis={q.Basis}");
        return new WebsiteTrafficResult(
            q.HumanHits,
            botHits,
            totalHits,
            page,
            total,
            avg,
            peak,
            q.Basis,
            seconds,
            $"hits={totalHits} (bots={botHits}) page={page.Display} → {total.Display} avg {avg.Display} over {Label(q.Basis)}");
    }

    public static int Seconds(BandwidthBasis basis)
        => basis switch
        {
            BandwidthBasis.Day => 86_400,
            BandwidthBasis.Year365 => 31_536_000,
            _ => 2_592_000
        };

    static decimal BitsPerUnit(DataUnit unit)
        => unit switch
        {
            DataUnit.Bit => 1m,
            DataUnit.Byte => 8m,
            DataUnit.Kb => 1_000m,
            DataUnit.KB => 8_000m,
            DataUnit.Kib => 1_024m,
            DataUnit.KiB => 8_192m,
            DataUnit.Mb => 1_000_000m,
            DataUnit.MB => 8_000_000m,
            DataUnit.Mib => 1_048_576m,
            DataUnit.MiB => 8_388_608m,
            DataUnit.Gb => 1_000_000_000m,
            DataUnit.GB => 8_000_000_000m,
            DataUnit.Gib => 1_073_741_824m,
            DataUnit.GiB => 8_589_934_592m,
            DataUnit.Tb => 1_000_000_000_000m,
            DataUnit.TB => 8_000_000_000_000m,
            DataUnit.Tib => 1_099_511_627_776m,
            DataUnit.TiB => 8_796_093_022_208m,
            _ => throw new ArgumentOutOfRangeException(nameof(unit))
        };

    static string Format(decimal bits, DataUnit unit)
    {
        var value = bits / BitsPerUnit(unit);
        var text = value == decimal.Truncate(value)
            ? decimal.Truncate(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.####", CultureInfo.InvariantCulture);
        return text + " " + Symbol(unit);
    }

    static string Symbol(DataUnit unit)
        => unit switch
        {
            DataUnit.Bit => "b",
            DataUnit.Byte => "B",
            DataUnit.Kb => "kb",
            DataUnit.KB => "kB",
            DataUnit.Kib => "Kib",
            DataUnit.KiB => "KiB",
            DataUnit.Mb => "Mb",
            DataUnit.MB => "MB",
            DataUnit.Mib => "Mib",
            DataUnit.MiB => "MiB",
            DataUnit.Gb => "Gb",
            DataUnit.GB => "GB",
            DataUnit.Gib => "Gib",
            DataUnit.GiB => "GiB",
            DataUnit.Tb => "Tb",
            DataUnit.TB => "TB",
            DataUnit.Tib => "Tib",
            DataUnit.TiB => "TiB",
            _ => unit.ToString()
        };

    static string Label(BandwidthBasis basis)
        => basis switch
        {
            BandwidthBasis.Day => "1 day",
            BandwidthBasis.Year365 => "365-day year",
            _ => "30-day month"
        };
}
