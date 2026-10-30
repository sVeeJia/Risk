using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command.Interfaces;
using Risk.Api.DesignPatterns.State;
using Risk.Api.Engine;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Command;

public sealed class EndTurnCommand : IGameCommand
{
    private readonly string _playerId;

    public EndTurnCommand(string playerId) => _playerId = playerId;

    public CommandResult Execute(Game game, EventLog log)
    {
        var error = GamePhaseStates.For(game.Phase).CheckCanEndTurn()
                    ?? TurnRules.CheckIsPlayersTurn(game, _playerId);
        if (error is not null) return CommandResult.Fail(error);

        TurnFlow.Advance(game, log);
        return CommandResult.Ok();
    }
}