using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risk.Api.Dtos.Mappers;
using Risk.Api.Dtos.Requests;
using Risk.Api.Dtos.Responses;
using Risk.Api.Services.Interfaces;

namespace Risk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/games")]
public sealed class GamesController : ControllerBase
{
    private readonly IGameService _games;

    public GamesController(IGameService games) => _games = games;

    [HttpPost]
    public async Task<ActionResult<GameResponse>> Start([FromBody] StartGame request, CancellationToken ct)
    {
        var userId = User.FindFirstValue("sub");
        if (userId is null) return Unauthorized();

        var displayName = User.FindFirstValue("name") ?? "Commander";
        var result = await _games.StartGameAsync(userId, displayName, request.StartingCountryId, ct);

        if (result.Status == StartGameStatus.InvalidCountry)
        {
            return BadRequest(new { message = "That country is not available for play." });         
        }

        return CreatedAtAction(nameof(Get), new { id = result.Game!.Id }, GameMapper.ToResponse(result.Game));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GameResponse>> Get(string id, CancellationToken ct)
    {
        var userId = User.FindFirstValue("sub");
        if (userId is null) return Unauthorized();
        if (!Guid.TryParse(id, out _)) return NotFound();

        var game = await _games.GetAsync(userId, id, ct);
        return game is null ? NotFound() : Ok(GameMapper.ToResponse(game));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GameSummaryResponse>>> List(CancellationToken ct)
    {
        var userId = User.FindFirstValue("sub");
        if (userId is null) return Unauthorized();

        var games = await _games.ListAsync(userId, ct);
        return Ok(games.Select(GameMapper.ToSummary).ToList());
    }

    [HttpGet("{id}/events")]
    public async Task<ActionResult<IReadOnlyList<GameEventResponse>>> Events(string id, [FromQuery] EventQuery query, CancellationToken ct)
    {
        var userId = User.FindFirstValue("sub");
        if (userId is null) return Unauthorized();
        if (!Guid.TryParse(id, out _)) return NotFound();

        var events = await _games.GetEventsAsync(userId, id, query.After, query.Limit, ct);
        return events is null ? NotFound() : Ok(events.Select(GameEventMapper.ToResponse).ToList());
    }
}