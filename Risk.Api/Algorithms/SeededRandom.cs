using Risk.Api.Algorithms.Interfaces;

namespace Risk.Api.Algorithms;

// Deterministic, counter-based random numbers (SplitMix64).
// The n-th number depends only on (seed, n), so a game can be reproduced by storing just the seed and how many numbers have been drawn.
public sealed class SeededRandom : IRandomSource
{
    private const ulong Gamma = 0x9E3779B97F4A7C15UL;
    private readonly long _seed;
    private long _position;

    public SeededRandom(long seed, long position = 0)
    {
        if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
        _seed = seed;
        _position = position;
    }

    // How many numbers have been drawn so far (stored later as RngRollsUsed).
    public long Position => _position;

    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));

        var n = (ulong)maxExclusive;

        // Rejection sampling removes modulo bias, so every value is equally likely.
        var threshold = unchecked((0UL - n) % n);
        ulong value;
        do { value = NextUInt64(); } while (value < threshold);

        return (int)(value % n);
    }

    private ulong NextUInt64()
    {
        _position++;
        return unchecked(Mix((ulong)_seed + (ulong)_position * Gamma));
    }

    private static ulong Mix(ulong z)
    {
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}