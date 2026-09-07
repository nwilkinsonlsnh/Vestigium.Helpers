using System.Numerics;

namespace Vestigium.Helpers.Analytics;

internal static class NumberConvert
{
    public static List<decimal> ToDecimalList<T>(IEnumerable<T> values)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(values);

        var list = new List<decimal>();
        var index = 0;
        foreach (var value in values)
        {
            list.Add(ToDecimal(value, index));
            index++;
        }

        if (list.Count == 0)
            throw new ArgumentException("A numeric series must contain at least one value.", nameof(values));

        return list;
    }

    public static decimal ToDecimal<T>(T value, int index)
        where T : INumber<T>
    {
        if (typeof(T) == typeof(decimal))
            return (decimal)(object)value!;

        if (typeof(T) == typeof(double))
        {
            var d = (double)(object)value!;
            if (!double.IsFinite(d))
                throw new ArgumentOutOfRangeException(nameof(value), $"Values[{index}] is not finite.");
            return (decimal)d;
        }

        if (typeof(T) == typeof(float))
        {
            var f = (float)(object)value!;
            if (!float.IsFinite(f))
                throw new ArgumentOutOfRangeException(nameof(value), $"Values[{index}] is not finite.");
            return (decimal)f;
        }

        if (typeof(T) == typeof(Half))
        {
            var h = (Half)(object)value!;
            var d = (double)h;
            if (!double.IsFinite(d))
                throw new ArgumentOutOfRangeException(nameof(value), $"Values[{index}] is not finite.");
            return (decimal)d;
        }

        try
        {
            return decimal.CreateChecked(value);
        }
        catch (OverflowException ex)
        {
            throw new ArgumentOutOfRangeException(nameof(value), ex, $"Values[{index}] cannot be stored as decimal.");
        }
    }
}
