using Risk.Api.Models;

namespace Risk.Api.Services.Interfaces;

public enum GameplayStatus
{
    Success,
    NotFound,
    Rejected,
    Conflict
}

public sealed record GameplayResult(GameplayStatus Status, string? Error = null, Game? Game = null, IReadOnlyList<GameEvent>? Events = null)
{
    public static GameplayResult NotFound() => new(GameplayStatus.NotFound);

    public static GameplayResult Rejected(string error) => new(GameplayStatus.Rejected, error);

    public static GameplayResult Conflict() => new(GameplayStatus.Conflict, "The game changed while you were playing. Reload it and try again.");

    public static GameplayResult Success(Game game, IReadOnlyList<GameEvent> events) => new(GameplayStatus.Success, null, game, events);
}

public interface IGameplayService
{
    Task<GameplayResult> StartAttackAsync(string userId, string gameId, string fromCountryId, string toCountryId, CancellationToken ct = default);
    Task<GameplayResult> RollAsync(string userId, string gameId, CancellationToken ct = default);
    Task<GameplayResult> StopAttackAsync(string userId, string gameId, CancellationToken ct = default);
    Task<GameplayResult> TransferAfterConquestAsync(string userId, string gameId, int soldiers, CancellationToken ct = default);
    Task<GameplayResult> PlaceReinforcementsAsync(string userId, string gameId, string countryId, int soldiers, CancellationToken ct = default);
    Task<GameplayResult> TransferAsync(string userId, string gameId, string fromCountryId, string toCountryId, int soldiers, CancellationToken ct = default);
    Task<GameplayResult> EndTurnAsync(string userId, string gameId, CancellationToken ct = default);
}