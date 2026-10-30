using Risk.Api.DesignPatterns.Factory;
using Risk.Api.Models;

namespace Risk.Tests.Helpers;

public static class TestGames
{
    // A fresh 10-country game where the human plays first. Soldiers are set explicitly,
    // so tests don't depend on which countries the draw picked.
    public static Game HumanFirst(int humanSoldiers = 5, int enemySoldiers = 3)
    {
        var countries = Enumerable.Range(1, 42)
            .Select(i => new Country { Id = $"C{i:00}", Name = $"Country {i}", Population = i * 5_000_000L, Rank = i })
            .ToList();

        var game = new GameFactory().Create("user-1", "Victor", "C01", countries, 1, DateTime.UtcNow);

        var human = game.Human();
        game.TurnQueue = new Queue<string>(new[] { human.Id }.Concat(game.Players.Where(p => !p.IsHuman).Select(p => p.Id)));

        foreach (var territory in game.Territories.Values) 
        {
            territory.Soldiers = territory.CountryId == human.StartingCountryId ? humanSoldiers : enemySoldiers;
        }
        return game;
    }

    public static Player Human(this Game game) => game.Players.Single(p => p.IsHuman);

    // The country of the first NPC player.
    public static string EnemyCountry(this Game game) => game.Players.First(p => !p.IsHuman).StartingCountryId;

    // Makes an NPC the current player.
    public static void GiveTurnToAnNpc(this Game game) => game.TurnQueue = new Queue<string>(
        game.Players.Where(p => !p.IsHuman).Select(p => p.Id).Concat(new[] { game.Human().Id }));
}