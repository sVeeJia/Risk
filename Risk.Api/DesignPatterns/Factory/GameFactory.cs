using Risk.Api.Algorithms;
using Risk.Api.DesignPatterns.Factory.Interfaces;
using Risk.Api.Engine;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Factory;

public sealed class GameFactory : IGameFactory
{
    public Game Create(
        string ownerUserId,
        string ownerDisplayName,
        string chosenCountryId,
        IReadOnlyList<Country> pool,
        long seed,
        DateTime nowUtc)
    {
        var random = new SeededRandom(seed);

        // Chosen country first, then 9 random ones (no duplicates).
        var countries = CountryDraw.Draw(chosenCountryId, pool, random);

        var territories = countries.ToDictionary(
            c => c.Id,
            c =>
            {
                var soldiers = SoldierAllocator.GetSoldiers(c.Population);
                return new Territory
                {
                    CountryId = c.Id,
                    Name = c.Name,
                    BaseSoldiers = soldiers,
                    Soldiers = soldiers
                };
            });

        // Index 0 is the human, the rest are NPCs, one country each.
        var players = countries
            .Select((c, index) => new Player
            {
                Id = Guid.NewGuid().ToString(),
                Name = index == 0 ? ownerDisplayName : $"{c.Name} (NPC)",
                IsHuman = index == 0,
                StartingCountryId = c.Id,
                OwnedCountryIds = new HashSet<string> { c.Id },
                PendingReinforcements = 0,
                IsEliminated = false
            })
            .ToList();

        // Random turn order: whoever ends up first plays first.
        var turnOrder = FisherYatesShuffle.Shuffled(players.Select(p => p.Id), random);

        return new Game
        {
            Id = Guid.NewGuid().ToString(),
            OwnerUserId = ownerUserId,
            Phase = GamePhase.Action,
            Round = 1,
            Territories = territories,
            Players = players,
            TurnQueue = new Queue<string>(turnOrder),
            RngSeed = seed,
            RngRollsUsed = random.Position,
            Version = 0,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
    }
}