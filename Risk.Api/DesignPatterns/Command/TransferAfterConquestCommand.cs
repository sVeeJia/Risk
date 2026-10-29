using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command.Interfaces;
using Risk.Api.DesignPatterns.State;
using Risk.Api.Engine;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Command;

public sealed class TransferAfterConquestCommand : IGameCommand
{
    private readonly string _playerId;
    private readonly int _soldiers;

    public TransferAfterConquestCommand(string playerId, int soldiers)
    {
        _playerId = playerId;
        _soldiers = soldiers;
    }

    public CommandResult Execute(Game game, EventLog log)
    {
        var error = GamePhaseStates.For(game.Phase).CheckCanTransferAfterConquest() ?? TurnRules.CheckIsPlayersTurn(game, _playerId);
        if (error is not null) 
        {
            return CommandResult.Fail(error);
        }

        var attack = game.ActiveAttack;
        var player = TurnRules.FindPlayer(game, _playerId);
        if (attack is null || player is null)
        {
            return CommandResult.Fail("There is no conquered country waiting for soldiers.");
        }
            

        var from = game.Territories[attack.AttackerCountryId];
        var to = game.Territories[attack.DefenderCountryId];

        if (_soldiers < 1)
        {
            return CommandResult.Fail("Move at least 1 soldier into the conquered country.");
        }
            

        // The attacking country must keep at least 1 soldier.
        var movable = from.Soldiers - 1;
        if (_soldiers > movable)
        {
            return CommandResult.Fail($"You must keep at least 1 soldier in {from.Name}. You can move up to {movable}.");
        }
        from.Soldiers -= _soldiers;
        to.Soldiers += _soldiers;

        log.Append(
            EventType.Transfer,
            player.Id,
            $"{player.Name} moves {_soldiers} soldiers from {from.Name} into {to.Name}.",
            data: new Dictionary<string, string>
            {
                ["fromCountryId"] = from.CountryId,
                ["toCountryId"] = to.CountryId,
                ["soldiers"] = _soldiers.ToString()
            });

        AttackSteps.End(game, log, player, AttackEndReason.Conquest);
        return CommandResult.Ok();
    }
}