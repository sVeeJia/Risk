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
[Route("api/games/{id}")]
public sealed class GameActionsController : ControllerBase
{
    private readonly IGameplayService _gameplay;

    public GameActionsController(IGameplayService gameplay) => _gameplay = gameplay;

    [HttpPost("attack")]
    public Task<ActionResult<ActionResponse>> Attack(string id, [FromBody] StartAttack request, CancellationToken ct) =>
        Run(id, userId => _gameplay.StartAttackAsync(userId, id, request.FromCountryId, request.ToCountryId, ct));

    [HttpPost("attack/roll")]
    public Task<ActionResult<ActionResponse>> Roll(string id, CancellationToken ct) =>
        Run(id, userId => _gameplay.RollAsync(userId, id, ct));

    [HttpPost("attack/stop")]
    public Task<ActionResult<ActionResponse>> Stop(string id, CancellationToken ct) =>
        Run(id, userId => _gameplay.StopAttackAsync(userId, id, ct));

    [HttpPost("attack/transfer")]
    public Task<ActionResult<ActionResponse>> Transfer(string id, [FromBody] MoveSoldiers request, CancellationToken ct) =>
        Run(id, userId => _gameplay.TransferAfterConquestAsync(userId, id, request.Soldiers, ct));

    [HttpPost("reinforcements")]
    public Task<ActionResult<ActionResponse>> Reinforce(string id, [FromBody] PlaceReinforcements request, CancellationToken ct) =>
        Run(id, userId => _gameplay.PlaceReinforcementsAsync(userId, id, request.CountryId, request.Soldiers, ct));

    [HttpPost("transfer")]
    public Task<ActionResult<ActionResponse>> MoveSoldiers(string id, [FromBody] TransferSoldiers request, CancellationToken ct) =>
        Run(id, userId => _gameplay.TransferAsync(userId, id, request.FromCountryId, request.ToCountryId, request.Soldiers, ct));

    [HttpPost("end-turn")]
    public Task<ActionResult<ActionResponse>> EndTurn(string id, CancellationToken ct) =>
        Run(id, userId => _gameplay.EndTurnAsync(userId, id, ct));
        
    [HttpPost("npc-turn")]
    public Task<ActionResult<ActionResponse>> NpcTurn(string id, CancellationToken ct) =>
        Run(id, userId => _gameplay.PlayNpcTurnAsync(userId, id, ct));
        
    private async Task<ActionResult<ActionResponse>> Run(string id, Func<string, Task<GameplayResult>> action)
    {
        var userId = User.FindFirstValue("sub");
        if (userId is null) return Unauthorized();
        if (!Guid.TryParse(id, out _)) return NotFound();

        var result = await action(userId);

        return result.Status switch
        {
            GameplayStatus.Success => Ok(new ActionResponse(GameMapper.ToResponse(result.Game!),
            result.Events!.Select(GameEventMapper.ToResponse).ToList())),
            
            GameplayStatus.NotFound => NotFound(),
            GameplayStatus.Conflict => Conflict(new { message = result.Error }),
            _ => BadRequest(new { message = result.Error })
        };
    }
}