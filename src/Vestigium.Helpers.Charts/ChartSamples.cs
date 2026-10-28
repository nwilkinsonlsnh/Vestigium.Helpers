using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Charts;

/// <summary>
/// Named demo series for the gallery: a symmetric bell, a right tail, a left tail.
/// Seeded so screenshots and tests stay still. Not a process model.
/// </summary>
public static class ChartSamples
{
    public const int DefaultSeed = 20260908;

    public static NumericSeries Symmetric(int n = 240, int seed = DefaultSeed)
        => NumericSeries.From(Normal(n, mean: 12, sd: 1.15, seed), "symmetric");

    public static NumericSeries RightTail(int n = 240, int seed = DefaultSeed)
    {
        var values = Normal(n, mean: 0, sd: 0.55, seed)
            .Select(z => Math.Exp(z) * 8 + 6)
            .ToArray();
        return NumericSeries.From(values, "right-tail");
    }

    public static NumericSeries LeftTail(int n = 240, int seed = DefaultSeed)
    {
        var right = RightTail(n, seed).Values;
        var hinge = 40m;
        var values = right.Select(v => hinge - v).ToArray();
        return NumericSeries.From(values, "left-tail");
    }

    private static double[] Normal(int n, double mean, double sd, int seed)
    {
        if (n < 2)
            throw new ArgumentOutOfRangeException(nameof(n), "Need at least two observations.");
        var rng = new Random(seed);
        var xs = new double[n];
        for (var i = 0; i < n; i++)
            xs[i] = NextNormal(rng, mean, sd);
        return xs;
    }

    private static double NextNormal(Random rng, double mean, double sd)
    {
        var u1 = 1d - rng.NextDouble();
        var u2 = 1d - rng.NextDouble();
        var z = Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
        return mean + sd * z;
    }
}
