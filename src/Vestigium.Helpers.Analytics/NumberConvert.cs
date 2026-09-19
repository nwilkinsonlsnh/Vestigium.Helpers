using System.Numerics;
using Vestigium.Logging;

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
        {
            AnalyticsLog.Error(
                AnalyticsEvents.SeriesRejectedEmpty,
                VestigiumStatus.Failed,
                AnalyticsCatalog.Subcategories.Series,
                "rejected empty series");
            throw new ArgumentException("A numeric series must contain at least one value.", nameof(values));
        }

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
            if (double.IsFinite(d)) return (decimal)d;
            RejectNonFinite(index);
            throw new ArgumentOutOfRangeException(nameof(value), $"Values[{index}] is not finite.");
        }

        if (typeof(T) == typeof(float))
        {
            var f = (float)(object)value!;
            if (!float.IsFinite(f))
            {
                RejectNonFinite(index);
                throw new ArgumentOutOfRangeException(nameof(value), $"Values[{index}] is not finite.");
            }
            return (decimal)f;
        }

        if (typeof(T) == typeof(Half))
        {
            var h = (Half)(object)value!;
            var d = (double)h;
            if (double.IsFinite(d)) return (decimal)d;
            RejectNonFinite(index);
            throw new ArgumentOutOfRangeException(nameof(value), $"Values[{index}] is not finite.");
        }

        try
        {
            return decimal.CreateChecked(value);
        }
        catch (OverflowException ex)
        {
            AnalyticsLog.Error(
                AnalyticsEvents.SeriesRejectedOverflow,
                VestigiumStatus.Failed,
                AnalyticsCatalog.Subcategories.Series,
                "rejected overflow",
                ex,
                properties: AnalyticsLog.Props(("index", index.ToString())));
            throw new ArgumentOutOfRangeException(nameof(value), ex, $"Values[{index}] cannot be stored as decimal.");
        }
    }

    private static void RejectNonFinite(int index)
        => AnalyticsLog.Error(
            AnalyticsEvents.SeriesRejectedNonFinite,
            VestigiumStatus.Failed,
            AnalyticsCatalog.Subcategories.Series,
            "rejected non-finite value",
            properties: AnalyticsLog.Props(("index", index.ToString())));
}
