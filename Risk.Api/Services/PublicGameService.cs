using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Models;
using Risk.Api.Services.Interfaces;

namespace Risk.Api.Services;

public sealed class PublicGameService : IPublicGameService
{
    private const int MaxEventsPerRequest = 500;

    private readonly IGameRepository _games;
    private readonly IGameEventRepository _events;

    public PublicGameService(IGameRepository games, IGameEventRepository events)
    {
        _games = games;
        _events = events;
    }

    public Task<Game?> GetGameAsync(string gameId, CancellationToken ct = default) =>
        _games.GetByIdAsync(gameId, ct);

    public async Task<IReadOnlyList<GameEvent>?> GetEventsAsync(string gameId, int after, int limit, CancellationToken ct = default)
    {
        if (await _games.GetByIdAsync(gameId, ct) is null)
        {
            return null;
        }

        return await _events.ListAsync(gameId, Math.Max(after, 0), Math.Clamp(limit, 1, MaxEventsPerRequest), ct);
    }
}