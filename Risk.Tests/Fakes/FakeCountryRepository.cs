using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Models;

namespace Risk.Tests.Fakes;

public sealed class FakeCountryRepository : ICountryRepository
{
    public List<Country> Items { get; } = new();
    public Task<IReadOnlyList<Country>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Country>>(Items.OrderBy(c => c.Rank).ToList());
    public Task UpsertManyAsync(IEnumerable<Country> countries, CancellationToken ct = default)
    {
        Items.AddRange(countries);
        return Task.CompletedTask;
    }
}