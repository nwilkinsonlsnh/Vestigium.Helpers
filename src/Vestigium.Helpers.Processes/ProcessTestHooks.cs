namespace Vestigium.Helpers.Processes;

/// <summary>Test-only injection. Never point this at live ProgramData from tests.</summary>
public static class ProcessTestHooks
{
    public static string? CommentStorePath { get; set; }
    public static string? CampaignRoot { get; set; }
    public static Func<DateTimeOffset>? Now { get; set; }
    public static Exception? QueryWatchFault { get; set; }

    internal static DateTimeOffset Clock() => Now?.Invoke() ?? DateTimeOffset.Now;
}
