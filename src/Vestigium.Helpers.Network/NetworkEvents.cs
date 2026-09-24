namespace Vestigium.Helpers.Network;

/// <summary>Custom catalog block 14500–14999 (count by 5).</summary>
public static class NetworkEvents
{
    public const int BlockStart = 14500;
    public const int BlockEnd = 14999;

    public const int ProbeEnter = 14500;
    public const int ProbeComplete = 14505;
    public const int OperationEnter = 14510;
    public const int OperationComplete = 14515;
    public const int OperationFailed = 14520;
    public const int OperationWarning = 14525;
    public const int RouteDenied = 14530;
    public const int IcmpForbidden = 14535;
    public const int CampaignWindowMissed = 14540;
    public const int CampaignPathEscape = 14545;
    public const int DnsPeerMismatch = 14550;
    public const int OuiLookupRejected = 14555;
}
