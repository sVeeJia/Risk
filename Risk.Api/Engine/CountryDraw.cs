using Risk.Api.Algorithms;
using Risk.Api.Algorithms.Interfaces;
using Risk.Api.Models;

namespace Risk.Api.Engine;

public static class CountryDraw
{
    public const int CountriesPerGame = 10;

    // Returns 10 distinct countries: the player's chosen country first, then 9 drawn at random from the rest of the pool.
    public static IReadOnlyList<Country> Draw(string chosenId, IReadOnlyList<Country> pool, IRandomSource random)
    {
        var chosen = pool.FirstOrDefault(c => c.Id.Equals(chosenId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("The chosen country is not in the pool.", nameof(chosenId));

        var others = pool.Where(c => c.Id != chosen.Id).ToList();
        if (others.Count < CountriesPerGame - 1)
        {
            throw new ArgumentException("The pool is too small to draw a game.", nameof(pool));

        }

        var drawn = FisherYatesShuffle.Shuffled(others, random).Take(CountriesPerGame - 1);
        
        return new[] { chosen }.Concat(drawn).ToList();
    }
}