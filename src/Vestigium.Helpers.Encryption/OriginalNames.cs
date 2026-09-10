using System.Text;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Encryption;

internal static class OriginalNames
{
    public static string Validate(string? name)
    {
        var raw = HelperGuard.NotBlank(name, nameof(name));
        if (raw.Contains('\\') || raw.Contains('\0') || raw.Contains(".."))
            throw new ArgumentException("Original file name is not a bare file name.", nameof(name));
        var file = Path.GetFileName(raw);
        if (string.IsNullOrWhiteSpace(file))
            throw new ArgumentException("Value is required.", nameof(name));
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
