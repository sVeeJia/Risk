using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Models;

namespace Risk.Tests.Fakes;

public sealed class FakeGameEventRepository : IGameEventRepository
{
    public List<GameEvent> Items { get; } = new();

    public Task AppendAsync(IEnumerable<GameEvent> events, CancellationToken ct = default)
    {
        Items.AddRange(events);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<GameEvent>> ListAsync(string gameId, int afterSequence, int limit, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<GameEvent>>(Items.Where(e => e.GameId == gameId && e.Sequence > afterSequence)
            .OrderBy(e => e.Sequence)
            .Take(limit)
            .ToList());

    public Task DeleteByGamesAsync(IEnumerable<string> gameIds, CancellationToken ct = default)
    {
        var ids = gameIds.ToHashSet();
        Items.RemoveAll(e => ids.Contains(e.GameId));
        return Task.CompletedTask;
    }
    public Task<int> GetLastSequenceAsync(string gameId, CancellationToken ct = default) =>
        Task.FromResult(Items.Where(e => e.GameId == gameId).Select(e => e.Sequence).DefaultIfEmpty(0).Max());
}