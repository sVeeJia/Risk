using Risk.Api.Dtos.Requests;
using Risk.Api.Dtos.Responses;

namespace Risk.Api.Services.Interfaces;

public interface ICountryService
{
    Task<PagedResponse<CountryResponse>> SearchAsync(CountryQuery query, CancellationToken ct = default);
    Task<CountryResponse?> GetByIdAsync(string id, CancellationToken ct = default);
}