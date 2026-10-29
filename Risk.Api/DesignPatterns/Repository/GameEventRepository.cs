using MongoDB.Driver;
using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Repository;

public sealed class GameEventRepository : IGameEventRepository
{
    private readonly IMongoCollection<GameEvent> _events;

    public GameEventRepository(IMongoClient client, IConfiguration config)
    {
        var db = client.GetDatabase(config["Mongo:Database"] ?? "risk");
        _events = db.GetCollection<GameEvent>("game_events");

        // A game can never have two events with the same sequence number.
        var index = new CreateIndexModel<GameEvent>(Builders<GameEvent>.IndexKeys.Ascending(e => e.GameId).Ascending(e => e.Sequence),
        new CreateIndexOptions { Unique = true });
        _events.Indexes.CreateOne(index);
    }

    public async Task AppendAsync(IEnumerable<GameEvent> events, CancellationToken ct = default)
    {
        var list = events.ToList();
        if (list.Count > 0)
        {
            await _events.InsertManyAsync(list, cancellationToken: ct);           
        }
    }

    public async Task<IReadOnlyList<GameEvent>> ListAsync(
        string gameId, int afterSequence, int limit, CancellationToken ct = default) =>
        await _events.Find(e => e.GameId == gameId && e.Sequence > afterSequence)
                     .SortBy(e => e.Sequence)
                     .Limit(limit)
                     .ToListAsync(ct);

    public async Task DeleteByGamesAsync(IEnumerable<string> gameIds, CancellationToken ct = default)
    {
        var ids = gameIds.ToList();
        if (ids.Count == 0) return;
        await _events.DeleteManyAsync(Builders<GameEvent>.Filter.In(e => e.GameId, ids), ct);
    }
}