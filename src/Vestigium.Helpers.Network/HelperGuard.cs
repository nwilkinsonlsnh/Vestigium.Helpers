namespace Vestigium.Helpers.Network;

internal static class HelperGuard
{
    public static T NotNull<T>(T? value, string name) where T : class
    {
        if (value is not null)
            return value;
        HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Guard, nameof(NotNull), name + " is null");
        throw new ArgumentNullException(name);
    }

    public static string NotBlank(string? value, string name)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value;
        HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Guard, nameof(NotBlank), name + " is blank");
        throw new ArgumentException("Value is required.", name);
    }
}
