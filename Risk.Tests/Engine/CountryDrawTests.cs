using Risk.Api.Algorithms;
using Risk.Api.Engine;
using Risk.Api.Models;

namespace Risk.Tests.Engine;

public class CountryDrawTests
{
    private static List<Country> PlayableCountries(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new Country { Id = $"C{i:00}", Name = $"Country {i}", Population = i * 1_000_000L, Rank = i })
            .ToList();

    [Fact]
    public void Draw_ReturnsTenDistinctCountries_WithChosenFirst()
    {
        var result = CountryDraw.Draw("C07", PlayableCountries(42), new SeededRandom(1));

        Assert.Equal(10, result.Count);
        Assert.Equal("C07", result[0].Id);
        Assert.Equal(10, result.Select(c => c.Id).Distinct().Count());
    }

    [Fact]
    public void Draw_SameSeed_GivesSameGame()
    {
        var a = CountryDraw.Draw("C01", PlayableCountries(42), new SeededRandom(5)).Select(c => c.Id);
        var b = CountryDraw.Draw("C01", PlayableCountries(42), new SeededRandom(5)).Select(c => c.Id);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Draw_DifferentSeeds_GiveDifferentGames()
    {
        var a = CountryDraw.Draw("C01", PlayableCountries(42), new SeededRandom(1)).Select(c => c.Id);
        var b = CountryDraw.Draw("C01", PlayableCountries(42), new SeededRandom(2)).Select(c => c.Id);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Draw_EveryPlayableCountryCanBeDrawn()
    {
        var seen = new HashSet<string>();
        for (var seed = 0; seed < 300; seed++)
            foreach (var c in CountryDraw.Draw("C01", PlayableCountries(42), new SeededRandom(seed))) seen.Add(c.Id);

        Assert.Equal(42, seen.Count);
    }

    [Fact]
    public void Draw_ChosenCountryNotPlayable_Throws() =>
        Assert.Throws<ArgumentException>(() => CountryDraw.Draw("ZZ", PlayableCountries(42), new SeededRandom(1)));

    [Fact]
    public void Draw_TooFewPlayableCountries_Throws() =>
        Assert.Throws<ArgumentException>(() => CountryDraw.Draw("C01", PlayableCountries(9), new SeededRandom(1)));
}