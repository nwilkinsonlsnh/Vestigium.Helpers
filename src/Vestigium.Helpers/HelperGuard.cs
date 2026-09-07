using Vestigium.Logging;

namespace Vestigium.Helpers;

/// <summary>
/// Shared argument guards for the Helpers libraries.
/// </summary>
public static class HelperGuard
{
    public static string Identity => "Vestigium.Helpers";

    public static T NotNull<T>(T? value, string name) where T : class
        => value ?? throw new ArgumentNullException(name);

    public static string NotBlank(string? value, string name)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", name)
            : value;

    public static string Probe()
    {
        var app = HelperLog.AppIds.Core;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Core probe started.");
        _ = NotBlank("ok", "sample");
        HelperLog.Information(app, VestigiumStatus.Success, app, $"Core probe complete. Identity={Identity}");
        return Identity;
    }
}
