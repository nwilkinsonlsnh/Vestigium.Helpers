namespace Vestigium.Helpers.Kql;

/// <summary>Custom catalog block 14000–14499 (count by 5). Separate from the field <see cref="KqlCatalog"/>.</summary>
public static class KqlEvents
{
    public const int BlockStart = 14000;
    public const int BlockEnd = 14499;

    public const int ProbeEnter = 14000;
    public const int ProbeComplete = 14005;
    public const int SessionCreated = 14010;
    public const int QueryWarning = 14015;
    public const int QueryFailed = 14020;
}
