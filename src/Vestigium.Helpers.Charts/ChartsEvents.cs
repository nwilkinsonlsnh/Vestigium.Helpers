namespace Vestigium.Helpers.Charts;

/// <summary>
/// Custom catalog block 16500–16999 (count by 5).
/// </summary>
public static class ChartsEvents
{
    public const int BlockStart = 16500;
    public const int BlockEnd = 16999;

    public const int ProbeEnter = 16500;
    public const int ProbeComplete = 16505;
    public const int ChartEnter = 16510;
    public const int ChartBuilt = 16515;
    public const int ChartSaved = 16520;
    public const int ChartRejectedEmpty = 16525;
    public const int ChartRejectedXy = 16530;
    public const int ChartRejectedLimits = 16535;
    public const int ChartRejectedBlankPath = 16540;
    public const int ChartThrown = 16545;
}
