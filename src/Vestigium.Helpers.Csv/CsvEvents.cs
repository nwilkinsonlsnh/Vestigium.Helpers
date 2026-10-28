namespace Vestigium.Helpers.Csv;

/// <summary>Custom catalog block 11500–11999 (count by 5).</summary>
public static class CsvEvents
{
    public const int BlockStart = 11500;
    public const int BlockEnd = 11999;

    public const int ProbeEnter = 11500;
    public const int ProbeComplete = 11505;
    public const int SessionEnter = 11510;
    public const int SessionCreated = 11515;
    public const int SessionOpened = 11520;
    public const int SessionSaved = 11525;
    public const int SessionRejected = 11530;
    public const int SessionThrown = 11535;
    public const int ParseRejected = 11540;
    public const int WriteRejected = 11545;
    public const int CellNeutralized = 11550;
    public const int CellRejectedNonFinite = 11555;
    public const int WriteSeriesComplete = 11560;
}
