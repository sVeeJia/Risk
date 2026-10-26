using MongoDB.Driver;
using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Repository;

public sealed class CountryRepository : ICountryRepository
{
    private readonly IMongoCollection<Country> _countries;
    private readonly ILogger<CountryRepository> _logger;

    public CountryRepository(IMongoClient client, IConfiguration config, ILogger<CountryRepository> logger)
    {
        _logger = logger;
        var db = client.GetDatabase(config["Mongo:Database"] ?? "risk");
        _countries = db.GetCollection<Country>("countries");
    }

    public async Task<IReadOnlyList<Country>> GetAllAsync(CancellationToken ct = default) =>
        await _countries.Find(Builders<Country>.Filter.Empty)
                        .SortBy(c => c.Rank)
                        .ToListAsync(ct);

    public async Task UpsertManyAsync(IEnumerable<Country> countries, CancellationToken ct = default)
    {
        var writes = countries
            .Select(c => new ReplaceOneModel<Country>(Builders<Country>.Filter.Eq(x => x.Id, c.Id), c) { IsUpsert = true })
            .ToList();

        if (writes.Count > 0)
        {
            var result = await _countries.BulkWriteAsync(writes, cancellationToken: ct);
            _logger.LogInformation(
                "Country bulk write to {Collection}: matched {MatchedCount}, upserted {UpsertedCount}, modified {ModifiedCount}.",
                _countries.CollectionNamespace.FullName,
                result.MatchedCount, result.Upserts.Count, result.ModifiedCount);
        }
    }
}
