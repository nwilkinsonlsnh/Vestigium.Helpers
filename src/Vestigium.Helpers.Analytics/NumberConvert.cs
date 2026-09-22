using System.Numerics;
using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

internal static class NumberConvert
{
    internal const string DescriptorOverflowMessage =
        "A series descriptor overflowed the decimal range.";

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

    public static IReadOnlyList<T> Freeze<T>(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return Array.AsReadOnly(items.ToArray());
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
            LogOverflow(("index", index.ToString()));
            throw new ArgumentOutOfRangeException($"Values[{index}] cannot be stored as decimal.", ex);
        }
    }

    public static void ThrowDescriptorOverflow(OverflowException ex)
    {
        LogOverflow();
        throw new ArgumentOutOfRangeException(DescriptorOverflowMessage, ex);
    }

    private static void LogOverflow(params (string Key, string? Value)[] extra)
        => AnalyticsLog.Error(
            AnalyticsEvents.SeriesRejectedOverflow,
            VestigiumStatus.Failed,
            AnalyticsCatalog.Subcategories.Series,
            "rejected overflow",
            properties: extra.Length == 0 ? null : AnalyticsLog.Props(extra));

    private static void RejectNonFinite(int index)
        => AnalyticsLog.Error(
            AnalyticsEvents.SeriesRejectedNonFinite,
            VestigiumStatus.Failed,
            AnalyticsCatalog.Subcategories.Series,
            "rejected non-finite value",
            properties: AnalyticsLog.Props(("index", index.ToString())));
}
