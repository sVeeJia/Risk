using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Repository.Interfaces;

public interface ICountryRepository
{
    Task<IReadOnlyList<Country>> GetAllAsync(CancellationToken ct = default);
    Task UpsertManyAsync(IEnumerable<Country> countries, CancellationToken ct = default);
}