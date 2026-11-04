using Risk.Api.DesignPatterns.Factory;
using Risk.Api.Engine;
using Risk.Api.Models;

namespace Risk.Tests.DesignPatterns;

public class GameFactoryTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    private static List<Country> PlayableCountries() =>
        Enumerable.Range(1, 42)
            .Select(i => new Country { Id = $"C{i:00}", Name = $"Country {i}", Population = i * 5_000_000L, Rank = i })
            .ToList();

    private static Game Create(long seed = 1, string chosen = "C07") =>
        new GameFactory().Create("user-1", "Victor", chosen, PlayableCountries(), seed, Now);

    [Fact]
    public void Create_BuildsTenTerritoriesAndTenPlayers()
    {
        var game = Create();

        Assert.Equal(10, game.Territories.Count);
        Assert.Equal(10, game.Players.Count);
    }

    [Fact]
    public void Create_HasOneHumanOwningTheChosenCountry()
    {
        var game = Create(chosen: "C07");

        var human = Assert.Single(game.Players, p => p.IsHuman);
        Assert.Equal("Victor", human.Name);
        Assert.Equal("C07", human.StartingCountryId);
        Assert.Equal(new[] { "C07" }, human.OwnedCountryIds);
    }

    [Fact]
    public void Create_EveryPlayerOwnsExactlyTheirStartingCountry()
    {
        var game = Create();

        Assert.All(game.Players, p => Assert.Equal(new[] { p.StartingCountryId }, p.OwnedCountryIds));
        Assert.Equal(10, game.Players.Select(p => p.StartingCountryId).Distinct().Count());
    }

    [Fact]
    public void Create_SoldiersMatchThePopulationTable()
    {
        var game = Create();
        var countries = PlayableCountries().ToDictionary(c => c.Id);

        foreach (var territory in game.Territories.Values)
        {
            var expected = SoldierAllocator.GetSoldiers(countries[territory.CountryId].Population);
            Assert.Equal(expected, territory.BaseSoldiers);
            Assert.Equal(expected, territory.Soldiers);
        }
    }

    [Fact]
    public void Create_TurnQueueContainsEveryPlayerOnce()
    {
        var game = Create();

        Assert.Equal(10, game.TurnQueue.Count);
        Assert.Equal(
            game.Players.Select(p => p.Id).OrderBy(x => x),
            game.TurnQueue.OrderBy(x => x));
    }

    [Fact]
    public void Create_StartsInRoundOneActionPhaseWithNoReinforcements()
    {
        var game = Create();

        Assert.Equal(1, game.Round);
        Assert.Equal(GamePhase.Action, game.Phase);
        Assert.Equal(0, game.Version);
        Assert.Equal("user-1", game.OwnerUserId);
        Assert.All(game.Players, p => Assert.Equal(0, p.PendingReinforcements));
    }

    [Fact]
    public void Create_StoresTheSeedAndTheNumbersUsed()
    {
        var game = Create(seed: 123);

        Assert.Equal(123, game.RngSeed);
        Assert.True(game.RngRollsUsed > 0);
    }

    [Fact]
    public void Create_SameSeed_GivesTheSameCountriesAndTurnOrder()
    {
        var a = Create(seed: 9);
        var b = Create(seed: 9);

        Assert.Equal(a.Territories.Keys, b.Territories.Keys);

        string[] TurnOrderByCountry(Game g) => g.TurnQueue
            .Select(id => g.Players.Single(p => p.Id == id).StartingCountryId)
            .ToArray();
        Assert.Equal(TurnOrderByCountry(a), TurnOrderByCountry(b));
    }

    [Fact]
    public void Create_FirstPlayerVaries_AcrossSeeds()
    {
        var humanFirst = 0;
        for (var seed = 0; seed < 200; seed++)
        {
            var game = Create(seed);
            var first = game.Players.Single(p => p.Id == game.TurnQueue.Peek());
            if (first.IsHuman) humanFirst++;
        }

        // 1 human in 10 players: about 20 of 200 games.
        Assert.InRange(humanFirst, 5, 45);
    }

    [Fact]
    public void Create_ChosenCountryNotPlayable_Throws() => Assert.Throws<ArgumentException>(() => Create(chosen: "ZZ"));

    [Fact]
    public void Create_RoundStarterIsTheFirstPlayerInTheTurnQueue()
    {
        var game = Create();

        Assert.Equal(game.TurnQueue.Peek(), game.RoundStartPlayerId);
    }

}