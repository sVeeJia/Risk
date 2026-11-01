using Microsoft.AspNetCore.SignalR;
using Risk.Api.Services.Interfaces;

namespace Risk.Api.Realtime;

// Spectators connect here and join the group of the game they watch.
// There is deliberately no method that changes a game: playing only happens through the owner-only endpoints.
public sealed class GameHub : Hub
{
    private readonly IPublicGameService _publicGames;

    public GameHub(IPublicGameService publicGames) => _publicGames = publicGames;

    public async Task Watch(string gameId)
    {
        if (!Guid.TryParse(gameId, out _) || await _publicGames.GetGameAsync(gameId, Context.ConnectionAborted) is null)
        {
            throw new HubException("Game not found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GameHubGroups.For(gameId));
    }

    public Task StopWatching(string gameId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GameHubGroups.For(gameId));
}