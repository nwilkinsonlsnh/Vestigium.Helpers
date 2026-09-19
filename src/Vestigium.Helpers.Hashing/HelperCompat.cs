namespace Vestigium.Helpers.Hashing;

/// <summary>
/// Package-local stand-in for HelperGuard.NotBlank. Routes through HashingLog.
/// Not part of the public API.
/// </summary>
internal static class HelperGuard
{
    public static string NotBlank(string? value, string paramName)
        => HashingLog.RequireNotBlank(value, paramName);
}
