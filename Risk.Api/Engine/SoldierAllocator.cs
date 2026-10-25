namespace Risk.Api.Engine;

public static class SoldierAllocator
{
    private const int MaxSoldiers = 9;

    // "Up to X million" is inclusive.
    private static readonly (long UpTo, int Soldiers)[] Table =
    {
        (5_000_000, 1),
        (10_000_000, 2),
        (25_000_000, 3),
        (50_000_000, 4),
        (75_000_000, 5),
        (100_000_000, 6),
        (125_000_000, 7),
        (150_000_000, 8),
    };

    public static int GetSoldiers(long population)
    {
        if (population < 0)
            throw new ArgumentOutOfRangeException(nameof(population), "Population cannot be negative.");

        foreach (var (upTo, soldiers) in Table)
        {
            if (population <= upTo)
                return soldiers;
        }

        return MaxSoldiers; // "Anything more"
    }
}