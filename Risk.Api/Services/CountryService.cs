using Risk.Api.DesignPatterns.Repository.Interfaces;
using Risk.Api.Dtos.Requests;
using Risk.Api.Dtos.Responses;
using Risk.Api.Engine;
using Risk.Api.Models;
using Risk.Api.Services.Interfaces;

namespace Risk.Api.Services;

public sealed class CountryService : ICountryService
{
    private readonly ICountryRepository _repository;

    public CountryService(ICountryRepository repository) => _repository = repository;

    public async Task<PagedResponse<CountryResponse>> SearchAsync(CountryQuery q, CancellationToken ct = default)
    {
        IEnumerable<CountryResponse> items = (await _repository.GetAllAsync(ct)).Select(ToResponse);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim();
            items = items.Where(c =>
                c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.Id.Equals(term, StringComparison.OrdinalIgnoreCase));
        }
        if (q.Soldiers is { } s) items = items.Where(c => c.Soldiers == s);
        if (q.MinPopulation is { } min) items = items.Where(c => c.Population >= min);
        if (q.MaxPopulation is { } max) items = items.Where(c => c.Population <= max);

        var desc = q.Direction == "desc";
        items = (q.Sort, desc) switch
        {
            ("name", false) => items.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase),
            ("name", true) => items.OrderByDescending(c => c.Name, StringComparer.OrdinalIgnoreCase),
            ("population", false) => items.OrderBy(c => c.Population),
            ("population", true) => items.OrderByDescending(c => c.Population),
            (_, true) => items.OrderByDescending(c => c.Rank),
            _ => items.OrderBy(c => c.Rank),
        };

        var list = items.ToList();
        var page = list.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToList();
        return new PagedResponse<CountryResponse>(page, list.Count, q.Page, q.PageSize);
    }

    public async Task<CountryResponse?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var country = (await _repository.GetAllAsync(ct))
            .FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return country is null ? null : ToResponse(country);
    }

    private static CountryResponse ToResponse(Country c) =>
        new(c.Id, c.Name, c.Population, c.Rank, SoldierAllocator.GetSoldiers(c.Population));
}