using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command;
using Risk.Api.DesignPatterns.Command.Interfaces;
using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Services.Interfaces;

namespace Risk.Api.Services;

public sealed class GameplayService : IGameplayService
{
    private readonly IGameRepository _games;
    private readonly IGameEventRepository _events;

    public GameplayService(IGameRepository games, IGameEventRepository events)
    {
        _games = games;
        _events = events;
    }

    public Task<GameplayResult> StartAttackAsync(string userId, string gameId, string fromCountryId, string toCountryId, CancellationToken ct = default) =>
        ExecuteAsync(userId, gameId, playerId => new StartAttackCommand(playerId, fromCountryId, toCountryId), ct);

    public Task<GameplayResult> RollAsync(string userId, string gameId, CancellationToken ct = default) =>
        ExecuteAsync(userId, gameId, playerId => new RollAttackCommand(playerId), ct);

    public Task<GameplayResult> StopAttackAsync(string userId, string gameId, CancellationToken ct = default) =>
        ExecuteAsync(userId, gameId, playerId => new StopAttackCommand(playerId), ct);

    public Task<GameplayResult> TransferAfterConquestAsync(string userId, string gameId, int soldiers, CancellationToken ct = default) =>
        ExecuteAsync(userId, gameId, playerId => new TransferAfterConquestCommand(playerId, soldiers), ct);

    private async Task<GameplayResult> ExecuteAsync(string userId, string gameId, Func<string, IGameCommand> createCommand, CancellationToken ct)
    {
        var game = await _games.GetByIdAsync(gameId, ct);
        if (game is null || game.OwnerUserId != userId)
        {
            return GameplayResult.NotFound();
        }

        // The signed-in user plays the human player of their own game.
        var human = game.Players.Single(p => p.IsHuman);

        var log = new EventLog(game.Id, await _events.GetLastSequenceAsync(game.Id, ct));
        var result = createCommand(human.Id).Execute(game, log);
        if (!result.Success)
        {
            return GameplayResult.Rejected(result.Error!);
        }

        // Save the game first (with the version check), and only then the events,
        // so a collision never leaves duplicate event numbers behind.
        if (!await _games.UpdateAsync(game, ct))
        {
            return GameplayResult.Conflict();
        }
            
        await _events.AppendAsync(log.Events, ct);
        return GameplayResult.Success(game, log.Events);
    }
}