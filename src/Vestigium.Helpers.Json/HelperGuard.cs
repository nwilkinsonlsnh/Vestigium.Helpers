namespace Vestigium.Helpers.Json;

internal static class HelperGuard
{
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
}
