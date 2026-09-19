namespace Vestigium.Helpers.ClosedXml;

/// <summary>Custom catalog block 11000–11499 (count by 5).</summary>
public static class ClosedXmlEvents
{
    public const int BlockStart = 11000;
    public const int BlockEnd = 11499;

    public const int ProbeEnter = 11000;
    public const int ProbeComplete = 11005;
    public const int SessionEnter = 11010;
    public const int SessionCreated = 11015;
    public const int SessionOpened = 11020;
    public const int SessionSaved = 11025;
    public const int SessionMerged = 11030;
    public const int SessionRejected = 11035;
    public const int SessionThrown = 11040;
    public const int SheetEnter = 11045;
    public const int SheetWrote = 11050;
    public const int SheetRejected = 11055;
    public const int ChartQueued = 11060;
    public const int ChartRejected = 11065;
    public const int CellNeutralized = 11070;
    public const int CellRejectedNonFinite = 11075;
    public const int WriteSeriesComplete = 11080;
}
