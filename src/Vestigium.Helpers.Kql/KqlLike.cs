namespace Vestigium.Helpers.Kql;

internal static class KqlLike
{
    public static bool IsMatch(string value, string pattern)
        => Match(value, 0, pattern, 0);

    private static bool Match(string value, int vi, string pattern, int pi)
    {
        while (pi < pattern.Length)
        {
            var p = pattern[pi];
            if (p is '%' or '*')
            {
                while (pi < pattern.Length && pattern[pi] is '%' or '*')
                    pi++;
                if (pi == pattern.Length)
                    return true;
                while (vi <= value.Length)
                {
                    if (Match(value, vi, pattern, pi))
                        return true;
                    vi++;
                }

                return false;
            }

            if (vi >= value.Length)
                return false;

            if (p != '?' && !CharsEqual(value[vi], p))
                return false;

            vi++;
            pi++;
        }

        return vi == value.Length;
    }

    private static bool CharsEqual(char a, char b)
        => char.ToUpperInvariant(a) == char.ToUpperInvariant(b);
}
