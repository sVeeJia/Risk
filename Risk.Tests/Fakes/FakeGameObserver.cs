using Risk.Api.DesignPatterns.Observer.Interfaces;
using Risk.Api.Models;

namespace Risk.Tests.Fakes;

public sealed class FakeGameObserver : IGameObserver
{
    public List<(Game Game, IReadOnlyList<GameEvent> Events)> Calls { get; } = new();
    public bool Throw { get; set; }

    public Task OnGameChangedAsync(Game game, IReadOnlyList<GameEvent> events, CancellationToken ct = default)
    {
        if (Throw) throw new InvalidOperationException("The observer failed.");

        Calls.Add((game, events));
        return Task.CompletedTask;
    }
}