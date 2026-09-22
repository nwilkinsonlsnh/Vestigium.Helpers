namespace Vestigium.Helpers.Json;

/// <summary>
/// Test-only injection. Hosts never set this. Tests assign a temp folder so DefaultExportDirectory
/// never touches the real Desktop.
/// </summary>
internal static class JsonTestHooks
{
    internal static string? ExportRoot { get; set; }
}
