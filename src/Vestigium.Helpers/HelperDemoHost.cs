namespace Vestigium.Helpers;

/// <summary>
/// Shared entry for every Helpers CLI demo. Initializes Vestigium.Logging with
/// the helper APPID so JSONL lands in %ProgramData%\Vestigium\Logs\{APPID}\.
/// </summary>
public static class HelperDemoHost
{
    public static int Run(string appId, string identity, Action probe)
    {
        HelperLog.InitializeHost(appId);
        Console.WriteLine($"Vestigium.Helpers.{appId} CLI");
        Console.WriteLine($"Identity : {identity}");
        Console.WriteLine($"APPID    : {appId}");
        Console.WriteLine();

        try
        {
            probe();
            HelperLog.Information(appId, Vestigium.Logging.VestigiumStatus.Success, appId, "CLI demo finished.");
        }
        catch (Exception ex)
        {
            HelperLog.Error(appId, Vestigium.Logging.VestigiumStatus.Failed, appId, "CLI demo failed.", ex);
            Console.Error.WriteLine(ex);
            PrintLogs(appId);
            HelperLog.Shutdown();
            return 1;
        }

        PrintLogs(appId);
        HelperLog.Shutdown();
        return 0;
    }

    private static void PrintLogs(string appId)
    {
        HelperLog.Flush();
        var dir = HelperLog.LogDirectory;
        Console.WriteLine();
        Console.WriteLine("JSONL directory:");
        Console.WriteLine($"  {dir}");
        Console.WriteLine($"  (rolling file vestigium-{appId}-*.json)");
        Console.WriteLine();
        Console.WriteLine("Recent JSON Lines:");
        var lines = HelperLog.RecentJsonLines;
        if (lines.Count == 0)
        {
            Console.WriteLine("  (none)");
            return;
        }

        foreach (var line in lines)
            Console.WriteLine(line);
    }
}
