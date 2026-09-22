namespace Vestigium.Helpers.Kql;

public enum KqlTriState
{
    False = 0,
    True = 1,
    Unknown = 2
}

/// <summary>
/// Cell value. A missing, denied, or empty string is <see cref="Unknown"/> — never <c>""</c>.
/// </summary>
public readonly struct KqlValue
{
    public static KqlValue Unknown { get; } = new(true, KqlType.String, null);

    private KqlValue(bool isUnknown, KqlType type, object? raw)
    {
        IsUnknown = isUnknown;
        Type = type;
        Raw = raw;
    }

    public bool IsUnknown { get; }
    public KqlType Type { get; }
    public object? Raw { get; }

    public static KqlValue From(object? value)
    {
        if (value is null)
            return Unknown;
        if (value is string s)
            return string.IsNullOrWhiteSpace(s) ? Unknown : new(false, KqlType.String, s);
        return value switch
        {
            bool b => new(false, KqlType.Boolean, b),
            TimeSpan t => new(false, KqlType.TimeSpan, t),
            DateTimeOffset d => new(false, KqlType.DateTime, d),
            DateTime d => new(false, KqlType.DateTime, new DateTimeOffset(d)),
            byte or sbyte or short or ushort or int or uint or long or ulong =>
                new(false, KqlType.Integer, Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture)),
            float or double or decimal =>
                new(false, KqlType.Number, Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture)),
            _ => string.IsNullOrWhiteSpace(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture))
                ? Unknown
                : new(false, KqlType.String, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture))
        };
    }
}

public interface IKqlRow
{
    KqlValue Get(string canonical);
}
