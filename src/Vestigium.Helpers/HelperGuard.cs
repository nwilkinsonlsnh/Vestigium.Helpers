using Vestigium.Logging;

namespace Vestigium.Helpers;

/// <summary>
/// Shared argument contracts for the Helpers libraries.
/// Every rejection writes Error / Failed through <see cref="HelperLog"/> (a no-op
/// until a host initializes) and then throws. Callers never swallow the exception.
/// </summary>
public static class HelperGuard
{
    public static string Identity => "Vestigium.Helpers";

    public static T NotNull<T>(T? value, string name) where T : class
    {
        if (value is not null)
            return value;
        HelperLog.Reject($"{name} is null");
        throw new ArgumentNullException(name);
    }

    public static string NotBlank(string? value, string name)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value;
        HelperLog.Reject($"{name} is blank");
        throw new ArgumentException("Value is required.", name);
    }

    public static IReadOnlyList<T> NotEmpty<T>(IReadOnlyList<T>? value, string name)
    {
        var list = NotNull(value, name);
        if (list.Count > 0)
            return list;
        HelperLog.Reject($"{name} is empty");
        throw new ArgumentException("Value must contain at least one item.", name);
    }

    public static int InRange(int value, int minInclusive, string name)
    {
        if (value >= minInclusive)
            return value;
        HelperLog.Reject($"{name}={value} is below {minInclusive}");
        throw new ArgumentOutOfRangeException(name, $"{name} must be at least {minInclusive}.");
    }

    public static double Finite(double value, string name)
    {
        if (double.IsFinite(value))
            return value;
        HelperLog.Reject($"{name} is not finite");
        throw new ArgumentOutOfRangeException(name, "NaN and Infinity are not allowed.");
    }

    public static float Finite(float value, string name)
    {
        if (float.IsFinite(value))
            return value;
        HelperLog.Reject($"{name} is not finite");
        throw new ArgumentOutOfRangeException(name, "NaN and Infinity are not allowed.");
    }

    public static string FileExists(string? path, string name)
    {
        var target = NotBlank(path, name);
        if (File.Exists(target))
            return target;
        HelperLog.Reject($"{name} not found path={target}");
        throw new FileNotFoundException($"{name} was not found.", target);
    }

    public static void NotDisposed(bool disposed, object instance)
    {
        if (!disposed)
            return;
        HelperLog.Reject("instance is disposed");
        ObjectDisposedException.ThrowIf(true, instance);
    }

    public static void Require(bool condition, string name, string message)
    {
        if (condition)
            return;
        HelperLog.Reject($"{name}: {message}");
        throw new ArgumentException(message, name);
    }

    public static void RequireState(bool condition, string message)
    {
        if (condition)
            return;
        HelperLog.Reject(message);
        throw new InvalidOperationException(message);
    }

    public static string Probe()
    {
        var app = HelperLog.AppIds.Core;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Probe, "Probe");
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Core probe started.");
        _ = NotBlank("ok", "sample");
        HelperLog.Information(app, VestigiumStatus.Success, app, $"Core probe complete. Identity={Identity}");
        HelperLog.Exit(app, HelperLog.Subcategories.Probe, "Probe", $"Identity={Identity}");
        return Identity;
    }
}
