using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Repository.Interfaces;

public interface IGameEventRepository
{
    Task AppendAsync(IEnumerable<GameEvent> events, CancellationToken ct = default);

    // Events with a sequence number greater than afterSequence, oldest first.
    Task<IReadOnlyList<GameEvent>> ListAsync(string gameId, int afterSequence, int limit, CancellationToken ct = default);

    // Used only when a game itself is deleted.
    Task DeleteByGamesAsync(IEnumerable<string> gameIds, CancellationToken ct = default);
    
    // The highest sequence number saved for the game, or 0 when it has no events.
    Task<int> GetLastSequenceAsync(string gameId, CancellationToken ct = default);
}