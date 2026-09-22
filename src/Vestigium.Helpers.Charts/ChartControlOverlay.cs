using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Charts;

/// <summary>Maps Analytics run-rule indexes onto already-plotted X/Y. Charts does not compute rules.</summary>
internal static class ChartControlOverlay
{
    public static (double[] X, double[] Y) RulePoints(double[] xs, double[] ys, RunRuleReport? rules)
    {
        if (rules is null || rules.AllIndexes.Count == 0 || xs.Length != ys.Length)
            return ([], []);

        var rx = new List<double>();
        var ry = new List<double>();
        foreach (var i in rules.AllIndexes)
        {
            if ((uint)i >= (uint)ys.Length)
                continue;
            rx.Add(xs[i]);
            ry.Add(ys[i]);
        }

        return (rx.ToArray(), ry.ToArray());
    }
}
