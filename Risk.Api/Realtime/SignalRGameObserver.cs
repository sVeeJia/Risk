using Microsoft.AspNetCore.SignalR;
using Risk.Api.DesignPatterns.Observer.Interfaces;
using Risk.Api.Dtos.Mappers;
using Risk.Api.Dtos.Responses;
using Risk.Api.Models;

namespace Risk.Api.Realtime;

// Pushes every change to everyone watching that game, using the same shape as the action endpoints return.
public sealed class SignalRGameObserver : IGameObserver
{
    public const string UpdateMethod = "GameUpdated";

    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<SignalRGameObserver> _logger;

    public SignalRGameObserver(IHubContext<GameHub> hub, ILogger<SignalRGameObserver> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task OnGameChangedAsync(Game game, IReadOnlyList<GameEvent> events, CancellationToken ct = default)
    {
        var update = new ActionResponse(
            GameMapper.ToResponse(game),
            events.Select(GameEventMapper.ToResponse).ToList());

        try
        {
            await _hub.Clients.Group(GameHubGroups.For(game.Id)).SendAsync(UpdateMethod, update, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not push the update for game {GameId} to its spectators.", game.Id);
        }
    }
}