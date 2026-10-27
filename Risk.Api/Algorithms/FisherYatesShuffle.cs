using Risk.Api.Algorithms.Interfaces;

namespace Risk.Api.Algorithms;

public static class FisherYatesShuffle
{
    // Shuffles the list in place. Every ordering is equally likely if the random source is fair.
    // (GeeksforGeeks, 2025)
    public static void Shuffle<T>(IList<T> items, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(random);

        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1); // 0..i inclusive
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    // Returns a new shuffled list and leaves the source untouched.
    public static List<T> Shuffled<T>(IEnumerable<T> source, IRandomSource random)
    {
        var copy = source.ToList();
        Shuffle(copy, random);
        return copy;
    }
}