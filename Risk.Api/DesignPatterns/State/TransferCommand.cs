using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command.Interfaces;
using Risk.Api.DesignPatterns.State;
using Risk.Api.Engine;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Command;

// Moves soldiers between two countries the player owns (no attack may be running).
public sealed class TransferCommand : IGameCommand
{
    private readonly string _playerId;
    private readonly string _fromCountryId;
    private readonly string _toCountryId;
    private readonly int _soldiers;

    public TransferCommand(string playerId, string fromCountryId, string toCountryId, int soldiers)
    {
        _playerId = playerId;
        _fromCountryId = fromCountryId.ToUpperInvariant();
        _toCountryId = toCountryId.ToUpperInvariant();
        _soldiers = soldiers;
    }

    public CommandResult Execute(Game game, EventLog log)
    {
        var error = GamePhaseStates.For(game.Phase).CheckCanTransfer()?? TurnRules.CheckIsPlayersTurn(game, _playerId);
        if (error is not null)
        {
         return CommandResult.Fail(error);
        }

        var player = TurnRules.FindPlayer(game, _playerId);
        if (player is null)
        {
            return CommandResult.Fail("Player not found in this game.");
        }

        if (_fromCountryId == _toCountryId)
        {
            return CommandResult.Fail("Choose two different countries.");
        }

        if (!player.OwnedCountryIds.Contains(_fromCountryId) || !player.OwnedCountryIds.Contains(_toCountryId) ||
            !game.Territories.TryGetValue(_fromCountryId, out var from) || !game.Territories.TryGetValue(_toCountryId, out var to))
        {
        return CommandResult.Fail("You can only move soldiers between countries you own.");
        }

        if (_soldiers < 1)
        {
            return CommandResult.Fail("Move at least 1 soldier.");
        }

        // Every country must keep at least 1 soldier.
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
            $"{player.Name} moves {_soldiers} soldiers from {from.Name} to {to.Name}.",
            data: new Dictionary<string, string>
            {
                ["fromCountryId"] = from.CountryId,
                ["toCountryId"] = to.CountryId,
                ["soldiers"] = _soldiers.ToString()
            });

        return CommandResult.Ok();
    }
}