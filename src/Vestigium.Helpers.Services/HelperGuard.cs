namespace Vestigium.Helpers.Services;

internal static class HelperGuard
{
    public static T NotNull<T>(T? value, string name) where T : class
    {
        if (value is not null)
            return value;
        HelperLog.Reject(name + " is null");
        throw new ArgumentNullException(name);
    }

    public static string NotBlank(string? value, string name)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value;
        HelperLog.Reject(name + " is blank");
        throw new ArgumentException("Value is required.", name);
    }

    public static IReadOnlyList<T> NotEmpty<T>(IReadOnlyList<T>? value, string name)
    {
        var list = NotNull(value, name);
        if (list.Count > 0)
            return list;
        HelperLog.Reject(name + " is empty");
        throw new ArgumentException("Value must contain at least one item.", name);
    }

    public static string FileExists(string? path, string name)
    {
        var target = NotBlank(path, name);
        if (File.Exists(target))
            return target;
        HelperLog.Reject(name + " not found path=" + target);
        throw new FileNotFoundException(name + " was not found.", target);
    }

    public static int InRange(int value, int minInclusive, string name)
    {
        if (value >= minInclusive)
            return value;
        HelperLog.Reject($"{name}={value} is below {minInclusive}");
        throw new ArgumentOutOfRangeException(name, $"{name} must be at least {minInclusive}.");
    }

    public static int AtMost(int value, int maxInclusive, string name)
    {
        if (value <= maxInclusive)
            return value;
        HelperLog.Reject($"{name}={value} is above {maxInclusive}");
        throw new ArgumentOutOfRangeException(name, $"{name} must be at most {maxInclusive}.");
    }

    public static void Require(bool condition, string name, string message)
    {
        if (condition)
            return;
        HelperLog.Reject(name + ": " + message);
        throw new ArgumentException(message, name);
    }

    public static void RequireState(bool condition, string message)
    {
        if (condition)
            return;
        HelperLog.Reject(message);
        throw new InvalidOperationException(message);
    }
}
