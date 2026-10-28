using System.Globalization;
using System.Net.NetworkInformation;

namespace Vestigium.Helpers.Kql;

/// <summary>
/// Library types a catalog subscribes to. Declaring the type unlocks its methods.
/// </summary>
public static class KqlTypes
{
    public static KqlType From(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        if (actual == typeof(bool)) return KqlType.Boolean;
        if (actual == typeof(char)) return KqlType.Char;
        if (actual == typeof(string)) return KqlType.String;
        if (actual == typeof(int) || actual == typeof(short) || actual == typeof(byte)) return KqlType.Integer;
        if (actual == typeof(long)) return KqlType.Long;
        if (actual == typeof(float) || actual == typeof(double)) return KqlType.Number;
        if (actual == typeof(decimal)) return KqlType.Decimal;
        if (actual == typeof(DateTime) || actual == typeof(DateTimeOffset) || actual == typeof(DateOnly)) return KqlType.DateTime;
        if (actual == typeof(TimeSpan)) return KqlType.TimeSpan;
        if (actual == typeof(KqlIpAddress)) return KqlType.IpAddress;
        if (actual == typeof(KqlMacAddress)) return KqlType.MacAddress;
        return KqlType.String;
    }

    public static KqlType Infer(object? value)
        => value is null ? KqlType.String : From(value.GetType());
}

/// <summary>IPv4 or IPv6 address. Subscribe with <see cref="KqlType.IpAddress"/> or <c>typeof(KqlIpAddress)</c>.</summary>
public readonly struct KqlIpAddress
{
    public KqlIpAddress(string text) => Text = text;

    public string Text { get; }

    public static bool TryParse(string? text, out KqlIpAddress value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (!System.Net.IPAddress.TryParse(text.Trim(), out _))
            return false;
        value = new KqlIpAddress(text.Trim());
        return true;
    }

    public override string ToString() => Text;
}

/// <summary>MAC address. Subscribe with <see cref="KqlType.MacAddress"/> or <c>typeof(KqlMacAddress)</c>.</summary>
public readonly struct KqlMacAddress
{
    public KqlMacAddress(string text) => Text = text;

    public string Text { get; }

    public static bool TryParse(string? text, out KqlMacAddress value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var body = text.Trim().Replace("-", ":", StringComparison.Ordinal);
        if (!PhysicalAddress.TryParse(body.Replace(":", "", StringComparison.Ordinal), out _))
            return false;
        value = new KqlMacAddress(body);
        return true;
    }

    public override string ToString() => Text;
}

/// <summary>Number with a unit. <c>500mb</c>, <c>1.5gb</c>, <c>100kb</c>.</summary>
public static class KqlUnits
{
    public static bool TryParse(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var body = text.Trim().ToLowerInvariant();
        var i = 0;
        while (i < body.Length && (char.IsDigit(body[i]) || body[i] == '.'))
            i++;
        if (i == 0 || !double.TryParse(body[..i], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return false;
        var unit = body[i..];
        var scale = unit switch
        {
            "" => 1d,
            "b" => 1d,
            "kb" => 1024d,
            "mb" => 1024d * 1024d,
            "gb" => 1024d * 1024d * 1024d,
            "tb" => 1024d * 1024d * 1024d * 1024d,
            _ => (double?)null
        };
        if (scale is null)
            return false;
        value = number * scale.Value;
        return true;
    }
}
