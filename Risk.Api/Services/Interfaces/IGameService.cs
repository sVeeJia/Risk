using Risk.Api.Models;

namespace Risk.Api.Services.Interfaces;

public enum StartGameStatus
{
    Success,
    InvalidCountry
}

public sealed record StartGameResult(StartGameStatus Status, Game? Game);

public interface IGameService
{
    Task<StartGameResult> StartGameAsync(string userId, string displayName, string chosenCountryId, CancellationToken ct = default);

    // Returns null when the game doesn't exist or belongs to someone else.
    Task<Game?> GetAsync(string userId, string gameId, CancellationToken ct = default);

    Task<IReadOnlyList<Game>> ListAsync(string userId, CancellationToken ct = default);
        // Returns null when the game doesn't exist or belongs to someone else.
    Task<IReadOnlyList<GameEvent>?> GetEventsAsync(string userId, string gameId, int afterSequence, int limit, CancellationToken ct = default);
}