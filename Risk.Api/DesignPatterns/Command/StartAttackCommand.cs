using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command.Interfaces;
using Risk.Api.DesignPatterns.State;
using Risk.Api.Engine;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Command;

public sealed class StartAttackCommand : IGameCommand
{
    private readonly string _playerId;
    private readonly string _fromCountryId;
    private readonly string _toCountryId;

    public StartAttackCommand(string playerId, string fromCountryId, string toCountryId)
    {
        _playerId = playerId;
        _fromCountryId = fromCountryId.ToUpperInvariant();
        _toCountryId = toCountryId.ToUpperInvariant();
    }

    public CommandResult Execute(Game game, EventLog log)
    {
        var error = GamePhaseStates.For(game.Phase).CheckCanStartAttack() ?? TurnRules.CheckIsPlayersTurn(game, _playerId);
        if (error is not null)
        { 
            return CommandResult.Fail(error);
        }

        if (game.AttackedThisTurn)
        {
            return CommandResult.Fail("You have already attacked this turn.");
        }

        var player = TurnRules.FindPlayer(game, _playerId);
        if (player is null)
        {
            return CommandResult.Fail("Player not found in this game.");
        }

        if (!player.OwnedCountryIds.Contains(_fromCountryId) || !game.Territories.TryGetValue(_fromCountryId, out var attacker))
        {
            return CommandResult.Fail("You can only attack from a country you own.");
        }

        if (!game.Territories.TryGetValue(_toCountryId, out var defender))
        {
            return CommandResult.Fail("That country is not in this game.");
        }

        if (player.OwnedCountryIds.Contains(_toCountryId))
        {
            return CommandResult.Fail("You cannot attack your own country.");

        }
          
        if (attacker.Soldiers < 2)
        {
            return CommandResult.Fail("You need at least 2 soldiers to attack.");
        }
            
        if (defender.Soldiers < 1)
        {
            return CommandResult.Fail("That country has no soldiers to fight.");
        }
            

        game.ActiveAttack = new AttackState
        {
            AttackerCountryId = attacker.CountryId,
            DefenderCountryId = defender.CountryId
        };
        
        game.Phase = GamePhase.Attacking;
        game.AttackedThisTurn = true;

        log.Append(
            EventType.AttackStarted,
            player.Id,
            $"{player.Name} attacks {defender.Name} from {attacker.Name}.",
            data: new Dictionary<string, string>
            {
                ["fromCountryId"] = attacker.CountryId,
                ["toCountryId"] = defender.CountryId,
                ["attackerSoldiers"] = attacker.Soldiers.ToString(),
                ["defenderSoldiers"] = defender.Soldiers.ToString()
            });

        return CommandResult.Ok();
    }
}