using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Observer.Interfaces;

// Anything that wants to know when a game has changed (for example, to tell the people watching it).
public interface IGameObserver
{
    Task OnGameChangedAsync(Game game, IReadOnlyList<GameEvent> events, CancellationToken ct = default);
}