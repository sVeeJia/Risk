using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Repository.Interfaces;

public interface IGameEventRepository
{
    Task AppendAsync(IEnumerable<GameEvent> events, CancellationToken ct = default);

    // Events with a sequence number greater than afterSequence, oldest first.
    Task<IReadOnlyList<GameEvent>> ListAsync(string gameId, int afterSequence, int limit, CancellationToken ct = default);

    // Used only when a game itself is deleted.
    Task DeleteByGamesAsync(IEnumerable<string> gameIds, CancellationToken ct = default);
}