namespace Vestigium.Helpers.Network;

public sealed record EchoWindow(TimeOnly LocalTime, int Count);

public sealed class IcmpEchoCampaignOptions
{
    public string Target { get; set; } = "";
    public DateOnly RangeStartDate { get; set; }
    public DateOnly RangeEndDate { get; set; }
    public string? TimeZoneId { get; set; }
    public List<EchoWindow> Windows { get; set; } = [];
    public string? ResultsPath { get; set; }
    public string? RecipePath { get; set; }
    public TimeSpan Grace { get; set; } = TimeSpan.FromMinutes(15);
    public IcmpEchoOptions Echo { get; set; } = new();
}

public sealed record IcmpEchoCampaignResult(
    string CampaignId,
    NetworkJobStatus Status,
    int WindowsRun,
    int WindowsMissed,
    int WindowsSkipped,
    int EchoesAppended,
    string ResultsPath);
