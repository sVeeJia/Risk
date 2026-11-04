using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risk.Api.Dtos.Mappers;
using Risk.Api.Dtos.Responses;
using Risk.Api.Services.Interfaces;
using Risk.Api.Dtos.Requests;
using Microsoft.AspNetCore.RateLimiting;

namespace Risk.Api.Controllers;

// Anyone with the link can watch. Nothing here can change a game.
[EnableRateLimiting("public")] 
[ApiController]
[AllowAnonymous]
[Route("api/public/games/{id}")]
public sealed class PublicGamesController : ControllerBase
{
    private readonly IPublicGameService _publicGames;

    public PublicGamesController(IPublicGameService publicGames) => _publicGames = publicGames;

    [HttpGet]
    public async Task<ActionResult<GameResponse>> Get(string id, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out _)) return NotFound();

        var game = await _publicGames.GetGameAsync(id, ct);
        if (game is null) return NotFound();

        Response.Headers.CacheControl = "no-store";
        return Ok(GameMapper.ToResponse(game));
    }

    [HttpGet("events")]
    public async Task<ActionResult<IReadOnlyList<GameEventResponse>>> Events(string id, [FromQuery] EventQuery query, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out _)) return NotFound();

        var events = await _publicGames.GetEventsAsync(id, query.After, query.Limit, ct);
        if (events is null) return NotFound();

        Response.Headers.CacheControl = "no-store";
        return Ok(events.Select(GameEventMapper.ToResponse).ToList());
    }
}