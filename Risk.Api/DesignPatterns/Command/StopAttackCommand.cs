using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command.Interfaces;
using Risk.Api.DesignPatterns.State;
using Risk.Api.Engine;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Command;

public sealed class StopAttackCommand : IGameCommand
{
    private readonly string _playerId;

    public StopAttackCommand(string playerId) => _playerId = playerId;

    public CommandResult Execute(Game game, EventLog log)
    {
        var error = GamePhaseStates.For(game.Phase).CheckCanStopAttack() ?? TurnRules.CheckIsPlayersTurn(game, _playerId);
        if (error is not null)
        {
            return CommandResult.Fail(error);
        }

        var player = TurnRules.FindPlayer(game, _playerId);
        if (game.ActiveAttack is null || player is null)
        {
            return CommandResult.Fail("There is no attack in progress.");
        }

        AttackSteps.End(game, log, player, AttackEndReason.Stopped);
        
        // Stopping an attack ends the player's turn.
        TurnFlow.Advance(game, log);
        
        return CommandResult.Ok();
    }
}