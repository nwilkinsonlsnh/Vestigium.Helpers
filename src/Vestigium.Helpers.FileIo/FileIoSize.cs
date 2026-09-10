using System.Globalization;
using Vestigium.Helpers;

namespace Vestigium.Helpers.FileIo;

public enum FileIoSizeUnit
{
    Byte = 0,
    KB = 1,
    KiB = 2,
    MB = 3,
    MiB = 4,
    GB = 5,
    GiB = 6,
    TB = 7,
    TiB = 8
}

/// <summary>
/// Caller-chosen size: integer/decimal + unit. 2048 MiB normalizes to 2 GiB.
/// SI (KB/MB/GB/TB) is 1000. IEC (KiB/MiB/GiB/TiB) is 1024.
/// </summary>
public readonly record struct FileIoSize(long Bytes, decimal InputValue, FileIoSizeUnit InputUnit)
{
    public string InputDisplay => Format(InputValue, InputUnit);
    public string Display => Normalize(Bytes);

    public static FileIoSize From(decimal value, FileIoSizeUnit unit)
    {
        if (value < 0)
        {
            HelperLog.Reject(HelperLog.AppIds.FileIo, "Size", nameof(From), $"value={value}");
            throw new ArgumentOutOfRangeException(nameof(value), "Size cannot be negative.");
        }

        var factor = Factor(unit);
        var bytesDec = decimal.Round(value * factor, 0, MidpointRounding.AwayFromZero);
        if (bytesDec > long.MaxValue)
        {
            HelperLog.Reject(HelperLog.AppIds.FileIo, "Size", nameof(From), "overflow");
            throw new ArgumentOutOfRangeException(nameof(value), "Size does not fit in 64-bit bytes.");
        }

        return new FileIoSize((long)bytesDec, value, unit);
    }

    public static FileIoSize FromBytes(long bytes) => new(bytes, bytes, FileIoSizeUnit.Byte);

    public static long Factor(FileIoSizeUnit unit)
        => unit switch
        {
            FileIoSizeUnit.Byte => 1,
            FileIoSizeUnit.KB => 1_000,
            FileIoSizeUnit.KiB => 1_024,
            FileIoSizeUnit.MB => 1_000_000,
            FileIoSizeUnit.MiB => 1_048_576,
            FileIoSizeUnit.GB => 1_000_000_000,
            FileIoSizeUnit.GiB => 1_073_741_824,
            FileIoSizeUnit.TB => 1_000_000_000_000,
            FileIoSizeUnit.TiB => 1_099_511_627_776,
            _ => throw new ArgumentOutOfRangeException(nameof(unit))
        };

    public static string Normalize(long bytes)
    {
        if (bytes < 0) bytes = 0;
        var iec = new (long Size, FileIoSizeUnit Unit)[]
        {
            (Factor(FileIoSizeUnit.TiB), FileIoSizeUnit.TiB),
            (Factor(FileIoSizeUnit.GiB), FileIoSizeUnit.GiB),
            (Factor(FileIoSizeUnit.MiB), FileIoSizeUnit.MiB),
            (Factor(FileIoSizeUnit.KiB), FileIoSizeUnit.KiB),
        };
        foreach (var (size, unit) in iec)
        {
            if (bytes >= size && bytes % size == 0)
                return Format(bytes / size, unit);
        }

        var si = new (long Size, FileIoSizeUnit Unit)[]
        {
            (Factor(FileIoSizeUnit.TB), FileIoSizeUnit.TB),
            (Factor(FileIoSizeUnit.GB), FileIoSizeUnit.GB),
            (Factor(FileIoSizeUnit.MB), FileIoSizeUnit.MB),
            (Factor(FileIoSizeUnit.KB), FileIoSizeUnit.KB),
        };
        foreach (var (size, unit) in si)
        {
            if (bytes >= size)
                return Format((decimal)bytes / size, unit);
        }

        return Format(bytes, FileIoSizeUnit.Byte);
    }

    static string Format(decimal value, FileIoSizeUnit unit)
    {
        var text = value == decimal.Truncate(value)
            ? decimal.Truncate(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.###", CultureInfo.InvariantCulture);
        return text + " " + unit;
    }
}
