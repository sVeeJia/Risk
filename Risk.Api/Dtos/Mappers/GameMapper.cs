using Risk.Api.Dtos.Responses;
using Risk.Api.Models;

namespace Risk.Api.Dtos.Mappers;

public static class GameMapper
{
    public static GameResponse ToResponse(Game game)
    {
        // Players are stored in slot order: the human is index 0, so Territory 1.
        var slotByCountry = game.Players
            .Select((p, index) => (p.StartingCountryId, Slot: index + 1))
            .ToDictionary(x => x.StartingCountryId, x => x.Slot);

        var ownerByCountry = game.Players
            .SelectMany(p => p.OwnedCountryIds.Select(country => (Country: country, Owner: p.Id)))
            .ToDictionary(x => x.Country, x => x.Owner);

        var territories = game.Territories.Values
            .Select(t => new TerritoryResponse(
                t.CountryId,
                t.Name,
                slotByCountry[t.CountryId],
                t.BaseSoldiers,
                t.Soldiers,
                ownerByCountry.GetValueOrDefault(t.CountryId)))
            .OrderBy(t => t.Slot)
            .ToList();

        var players = game.Players
            .Select(p => new PlayerResponse(p.Id, p.Name, p.IsHuman, p.StartingCountryId, p.OwnedCountryIds.Count, p.IsEliminated))
            .ToList();

        var currentPlayerId = game.TurnQueue.Count > 0 ? game.TurnQueue.Peek() : null;

        // The RNG seed is deliberately not part of the response.
        return new GameResponse(
            game.Id,
            game.Phase.ToString(),
            game.Round,
            currentPlayerId,
            game.WinnerPlayerId,
            territories,
            players,
            game.CreatedAt);
    }

    public static GameSummaryResponse ToSummary(Game game)
    {
        var human = game.Players.First(p => p.IsHuman);
        var territory = game.Territories[human.StartingCountryId];

        return new GameSummaryResponse(
            game.Id,
            game.Phase.ToString(),
            game.Round,
            human.StartingCountryId,
            territory.Name,
            game.CreatedAt);
    }
}