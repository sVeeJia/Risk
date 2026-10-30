using MongoDB.Driver;
using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Repository;

public sealed class GameRepository : IGameRepository
{
    private readonly IMongoCollection<Game> _games;

    public GameRepository(IMongoClient client, IConfiguration config)
    {
        var db = client.GetDatabase(config["Mongo:Database"] ?? "risk");
        _games = db.GetCollection<Game>("games");

        // Listing "my games" newest first needs this.
        var index = new CreateIndexModel<Game>(Builders<Game>.IndexKeys.Ascending(g => g.OwnerUserId).Descending(g => g.CreatedAt));
        _games.Indexes.CreateOne(index);
    }

    public Task CreateAsync(Game game, CancellationToken ct = default) => _games.InsertOneAsync(game, cancellationToken: ct);

    public async Task<Game?> GetByIdAsync(string id, CancellationToken ct = default) => await _games.Find(g => g.Id == id).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Game>> ListByOwnerAsync(string ownerUserId, CancellationToken ct = default) => await _games.Find(g => g.OwnerUserId == ownerUserId)
        .SortByDescending(g => g.CreatedAt)
        .ToListAsync(ct);

    public async Task<bool> UpdateAsync(Game game, CancellationToken ct = default)
    {
        var expectedVersion = game.Version;
        game.Version = expectedVersion + 1;
        game.UpdatedAt = DateTime.UtcNow;

        var result = await _games.ReplaceOneAsync(
            g => g.Id == game.Id && g.Version == expectedVersion,
            game,
            cancellationToken: ct);

        if (result.MatchedCount == 0)
        {
            game.Version = expectedVersion; // nothing was saved, undo the bump
            return false;
        }
        return true;
    }

    public async Task<IReadOnlyList<string>> DeleteUnplayedAsync(string ownerUserId, CancellationToken ct = default)
    {
        var unplayed = Builders<Game>.Filter.Where(g => g.OwnerUserId == ownerUserId && g.Version == 0);
        var ids = await _games.Find(unplayed).Project(g => g.Id).ToListAsync(ct);
        if (ids.Count == 0) return ids;

        await _games.DeleteManyAsync(Builders<Game>.Filter.In(g => g.Id, ids), ct);
        return ids;
    }
}