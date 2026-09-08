namespace Vestigium.Helpers.Encryption;

public sealed class EncryptionValidationResult
{
    public bool IsVestigium { get; init; }
    public bool HeaderPresent { get; init; }
    public bool TrailerPresent { get; init; }
    public bool HeaderTrailerAgree { get; init; }
    public bool? StructuralMacValid { get; init; }
    public bool HasHiddenOriginalName { get; init; }
    public string? OriginalFileName { get; init; }
    public EncryptionFileInfo? Info { get; init; }
    public IReadOnlyList<string> Problems { get; init; } = [];
}
