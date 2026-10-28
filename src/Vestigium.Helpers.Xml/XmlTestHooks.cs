namespace Vestigium.Helpers.Xml;

/// <summary>
/// Test-only injection. Hosts never set this. Tests assign a temp folder so DefaultExportDirectory
/// never touches the real Desktop.
/// </summary>
internal static class XmlTestHooks
{
    internal static string? ExportRoot { get; set; }

    internal static int ResolverAsks { get; set; }
}
