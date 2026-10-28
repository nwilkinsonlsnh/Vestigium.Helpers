namespace Vestigium.Helpers.FileIo;

public enum FileIoThreadCount
{
    One = 1,
    Eight = 8,
    Sixteen = 16,
    ThirtyTwo = 32,
    FortyEight = 48,
    SixtyFour = 64,
    Eighty = 80,
    NinetySix = 96,
    OneHundredTwelve = 112,
    OneHundredTwentyEight = 128,
}

public static class FileIoThreads
{
    public static readonly int[] Allowed = [1, 8, 16, 32, 48, 64, 80, 96, 112, 128];

    public static int Snap(int requested)
    {
        var best = Allowed[0];
        var distance = int.MaxValue;
        foreach (var count in Allowed)
        {
            var gap = Math.Abs(count - requested);
            if (gap < distance)
            {
                best = count;
                distance = gap;
            }
        }
        return best;
    }

    public static int[] Split(int requested)
    {
        var total = Snap(requested);
        if (total < 5)
            return Small(total);

        var raw = new[] { total * 0.70, total * 0.10, total * 0.10, total * 0.05, total * 0.05 };
        var shares = raw.Select(value => Math.Max(1, (int)Math.Round(value, MidpointRounding.AwayFromZero))).ToArray();
        var sum = shares.Sum();
        shares[0] += total - sum;
        if (shares[0] < 1)
            shares[0] = 1;
        return shares;
    }

    private static int[] Small(int total)
        => total switch
        {
            1 => [1, 0, 0, 0, 0],
            4 => [2, 1, 1, 0, 0],
            _ => [Math.Max(1, total - 2), 1, 0, 0, 1]
        };
}
