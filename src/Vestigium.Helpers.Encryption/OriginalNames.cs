using System.Text;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Encryption;

internal static class OriginalNames
{
    public static string Validate(string? name)
    {
        var file = Path.GetFileName(HelperGuard.NotBlank(name, nameof(name)));
        if (string.IsNullOrWhiteSpace(file))
            throw new ArgumentException("Value is required.", nameof(name));
        if (file.Contains('/') || file.Contains('\\') || file.Contains('\0') || file.Contains(".."))
            throw new ArgumentException("Original file name is not a bare file name.", nameof(name));
        if (Encoding.UTF8.GetByteCount(file) > 255)
            throw new ArgumentException("Original file name is too long.", nameof(name));
        return file;
    }

    public static string Stem(string originalFileName)
    {
        var file = Validate(originalFileName);
        var stem = Path.GetFileNameWithoutExtension(file);
        return string.IsNullOrWhiteSpace(stem) ? "file" : stem;
    }
}
