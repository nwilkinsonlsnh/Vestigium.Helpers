namespace Vestigium.Helpers.Processes;

/// <summary>Test-only injection. Never point this at live ProgramData from tests.</summary>
public static class ProcessTestHooks
{
    public static string? CommentStorePath { get; set; }
    public static string? CampaignRoot { get; set; }
}
