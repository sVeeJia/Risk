using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command.Interfaces;
using Risk.Api.DesignPatterns.State;
using Risk.Api.Engine;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Command;

public sealed class PlaceReinforcementsCommand : IGameCommand
{
    private readonly string _playerId;
    private readonly string _countryId;
    private readonly int _soldiers;

    public PlaceReinforcementsCommand(string playerId, string countryId, int soldiers)
    {
        _playerId = playerId;
        _countryId = countryId.ToUpperInvariant();
        _soldiers = soldiers;
    }

    public CommandResult Execute(Game game, EventLog log)
    {
        var error = GamePhaseStates.For(game.Phase).CheckCanPlaceReinforcements() ?? TurnRules.CheckIsPlayersTurn(game, _playerId);
        if (error is not null)
        {
             return CommandResult.Fail(error);
        }

        var player = TurnRules.FindPlayer(game, _playerId);
        if (player is null)
        {
            return CommandResult.Fail("Player not found in this game.");
        }

        if (!player.OwnedCountryIds.Contains(_countryId) || !game.Territories.TryGetValue(_countryId, out var territory))
        { 
           
            return CommandResult.Fail("You can only place reinforcements in a country you own.");
        }
        if (_soldiers < 1)
        {
            return CommandResult.Fail("Place at least 1 soldier.");
        }

        if (_soldiers > player.PendingReinforcements)
        {   
            return CommandResult.Fail($"You only have {player.PendingReinforcements} reinforcements left to place.");
        }
        
        territory.Soldiers += _soldiers;
        player.PendingReinforcements -= _soldiers;

        log.Append(
            EventType.ReinforcementsPlaced,
            player.Id,
            $"{player.Name} places {_soldiers} soldiers in {territory.Name}.",
            data: new Dictionary<string, string>
            {
                ["countryId"] = territory.CountryId,
                ["soldiers"] = _soldiers.ToString(),
                ["remaining"] = player.PendingReinforcements.ToString()
            });

        // Once every reinforcement is placed, the player can attack, move soldiers, or end the turn.
        if (player.PendingReinforcements == 0)
        {
            game.Phase = GamePhase.Action;
        }
        
        return CommandResult.Ok();
    }
}