using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risk.Api.Dtos.Requests;
using Risk.Api.Dtos.Responses;
using Risk.Api.Services.Interfaces;

namespace Risk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/countries")]
public sealed class CountriesController : ControllerBase
{
    private readonly ICountryService _countries;

    public CountriesController(ICountryService countries) => _countries = countries;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<CountryResponse>>> Search(
        [FromQuery] CountryQuery query, CancellationToken ct) =>
        Ok(await _countries.SearchAsync(query, ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<CountryResponse>> Get(string id, CancellationToken ct)
    {
        if (id.Length != 2 || !id.All(char.IsAsciiLetter)) return BadRequest("Country id must be a 2-letter ISO code.");
        var country = await _countries.GetByIdAsync(id, ct);
        return country is null ? NotFound() : Ok(country);
    }
}