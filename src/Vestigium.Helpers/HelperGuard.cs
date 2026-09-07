namespace Vestigium.Helpers;

/// <summary>
/// Shared argument guards for the Helpers libraries.
/// </summary>
public static class HelperGuard
{
    /// <summary>
    /// Returns the assembly identity so hosts and tests can prove the library loaded.
    /// </summary>
    public static string Identity => "Vestigium.Helpers";

    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is null.
    /// </summary>
    public static T NotNull<T>(T? value, string name) where T : class
        => value ?? throw new ArgumentNullException(name);

    /// <summary>
    /// Throws <see cref="ArgumentException"/> when <paramref name="value"/> is null or whitespace.
    /// </summary>
    public static string NotBlank(string? value, string name)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", name)
            : value;
}
