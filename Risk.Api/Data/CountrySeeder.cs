using Risk.Api.Data.Import;
using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Models;

namespace Risk.Api.Data;

public sealed class CountrySeeder
{
    private readonly ICountrySource _source;
    private readonly ICountryRepository _repository;
    private readonly ILogger<CountrySeeder> _logger;

    public CountrySeeder(ICountrySource source, ICountryRepository repository, ILogger<CountrySeeder> logger)
    {
        _source = source;
        _repository = repository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var countries = _source.Read()
            .Select(r => new Country
            {
                Id = r.Iso2,
                Name = r.Name,
                Population = r.Population,
                Rank = r.Rank
            })
            .ToList();

        await _repository.UpsertManyAsync(countries, ct);
        _logger.LogInformation("Seeded {Count} countries.", countries.Count);
    }
}