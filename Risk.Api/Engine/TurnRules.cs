using Risk.Api.Models;

namespace Risk.Api.Engine;

public static class TurnRules
{
    // Returns null when it is the player's turn, otherwise the reason it isn't.
    public static string? CheckIsPlayersTurn(Game game, string playerId) =>
        game.TurnQueue.Count > 0 && game.TurnQueue.Peek() == playerId ? null : "It is not your turn.";

    public static Player? FindPlayer(Game game, string playerId) =>
        game.Players.FirstOrDefault(p => p.Id == playerId);

    // The player who currently owns the country.
    public static Player? OwnerOf(Game game, string countryId) =>
        game.Players.FirstOrDefault(p => p.OwnedCountryIds.Contains(countryId));
}