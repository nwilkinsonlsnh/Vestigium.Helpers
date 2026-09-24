namespace Vestigium.Helpers.Json;

/// <summary>
/// Test-only injection. Hosts never set this. Tests assign a temp folder so DefaultExportDirectory
/// never touches the real Desktop. Size-cap overrides exist so tests do not write 32 MiB files.
/// </summary>
internal static class JsonTestHooks
{
    internal static string? ExportRoot { get; set; }

    internal static long? MaxDocumentBytes { get; set; }

    internal static int? MaxJsonlLineBytes { get; set; }
}
