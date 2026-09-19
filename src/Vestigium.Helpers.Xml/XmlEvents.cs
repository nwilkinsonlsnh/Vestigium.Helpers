namespace Vestigium.Helpers.Xml;

/// <summary>Custom catalog block 16500–16999 (count by 5).</summary>
public static class XmlEvents
{
    public const int BlockStart = 16500;
    public const int BlockEnd = 16999;

    public const int ProbeEnter = 16500;
    public const int ProbeComplete = 16505;
    public const int OperationEnter = 16510;
    public const int OperationComplete = 16515;
    public const int OperationFailed = 16520;
    public const int OperationWarning = 16525;
}
