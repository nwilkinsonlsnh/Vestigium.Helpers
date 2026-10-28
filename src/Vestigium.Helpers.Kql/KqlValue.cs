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
        return value switch
        {
            null => Unknown,
            string s => string.IsNullOrWhiteSpace(s) ? Unknown : new KqlValue(false, KqlType.String, s),
            _ => value switch
            {
                bool b => new KqlValue(false, KqlType.Boolean, b),
                TimeSpan t => new KqlValue(false, KqlType.TimeSpan, t),
                DateTimeOffset d => new KqlValue(false, KqlType.DateTime, d),
                DateTime d => new KqlValue(false, KqlType.DateTime, new DateTimeOffset(d)),
                byte or sbyte or short or ushort or int or uint or long or ulong => new KqlValue(false, KqlType.Integer,
                    Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture)),
                float or double or decimal => new KqlValue(false, KqlType.Number,
                    Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture)),
                _ => string.IsNullOrWhiteSpace(Convert.ToString(value,
                    System.Globalization.CultureInfo.InvariantCulture))
                    ? Unknown
                    : new KqlValue(false, KqlType.String,
                        Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture))
            }
        };
    }
}

public interface IKqlRow
{
    KqlValue Get(string canonical);
}
