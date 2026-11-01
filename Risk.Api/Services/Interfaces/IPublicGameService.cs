using Risk.Api.Models;

namespace Risk.Api.Services.Interfaces;

// Read-only access for anyone who has the link to a game.
public interface IPublicGameService
{
    Task<Game?> GetGameAsync(string gameId, CancellationToken ct = default);
    Task<IReadOnlyList<GameEvent>?> GetEventsAsync(string gameId, int after, int limit, CancellationToken ct = default);
}