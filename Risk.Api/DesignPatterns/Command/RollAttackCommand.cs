using Risk.Api.Algorithms;
using Risk.Api.Algorithms.Interfaces;
using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command.Interfaces;
using Risk.Api.DesignPatterns.State;
using Risk.Api.Engine;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Command;

public sealed class RollAttackCommand : IGameCommand
{
    private readonly string _playerId;
    private readonly IRandomSource? _random;

    // Normally the dice come from the game's own seed. Tests can pass scripted dice instead.
    public RollAttackCommand(string playerId, IRandomSource? random = null)
    {
        _playerId = playerId;
        _random = random;
    }

    public CommandResult Execute(Game game, EventLog log)
    {
        var error = GamePhaseStates.For(game.Phase).CheckCanRoll() ?? TurnRules.CheckIsPlayersTurn(game, _playerId);
        if (error is not null) 
        {
            return CommandResult.Fail(error);
        }

        var attack = game.ActiveAttack;
        var player = TurnRules.FindPlayer(game, _playerId);
        if (attack is null || player is null)
        {
            return CommandResult.Fail("There is no attack in progress.");            
        }

        var attacker = game.Territories[attack.AttackerCountryId];
        var defender = game.Territories[attack.DefenderCountryId];

        // Resume the game's dice exactly where they stopped.
        var seeded = _random is null ? new SeededRandom(game.RngSeed, game.RngRollsUsed) : null;
        var round = BattleEngine.ResolveRound(attacker.Soldiers, defender.Soldiers, _random ?? seeded!);
        if (seeded is not null) game.RngRollsUsed = seeded.Position;

        attacker.Soldiers -= round.AttackerLosses;
        defender.Soldiers -= round.DefenderLosses;

        log.Append(
            EventType.BattleRound,
            player.Id,
            $"{attacker.Name} [{string.Join(", ", round.AttackerDice)}] against {defender.Name} " +
            $"[{string.Join(", ", round.DefenderDice)}]. {attacker.Name} loses {round.AttackerLosses}, " +
            $"{defender.Name} loses {round.DefenderLosses}.",
            data: new Dictionary<string, string>
            {
                ["attackerDice"] = string.Join(",", round.AttackerDice),
                ["defenderDice"] = string.Join(",", round.DefenderDice),
                ["attackerLosses"] = round.AttackerLosses.ToString(),
                ["defenderLosses"] = round.DefenderLosses.ToString(),
                ["attackerSoldiers"] = attacker.Soldiers.ToString(),
                ["defenderSoldiers"] = defender.Soldiers.ToString()
            });

        switch (BattleEngine.GetStatus(attacker.Soldiers, defender.Soldiers))
        {
            case BattleStatus.Conquered:
                Conquer(game, log, player, attacker, defender);
                break;
            case BattleStatus.AttackerExhausted:
                AttackSteps.End(game, log, player, "the attacker is down to 1 soldier.");
                break;
        }

        return CommandResult.Ok();
    }

    private static void Conquer(Game game, EventLog log, Player player, Territory attacker, Territory defender)
    {
        var previousOwner = TurnRules.OwnerOf(game, defender.CountryId);

        // The reward comes from the conquered country's own soldier value (its base soldiers),
        // not from how many it had left, which is always 0 when it falls.
        var reward = BattleEngine.GetConquestReward(defender.BaseSoldiers);

        previousOwner?.OwnedCountryIds.Remove(defender.CountryId);
        player.OwnedCountryIds.Add(defender.CountryId);
        attacker.Soldiers += reward;

        log.Append(
            EventType.CountryConquered,
            player.Id,
            $"{player.Name} conquers {defender.Name} and gains {reward} soldiers in {attacker.Name}.",
            data: new Dictionary<string, string>
            {
                ["countryId"] = defender.CountryId,
                ["reward"] = reward.ToString(),
                ["previousOwnerId"] = previousOwner?.Id ?? string.Empty
            });

        if (previousOwner is not null && previousOwner.OwnedCountryIds.Count == 0)
        {
            previousOwner.IsEliminated = true;
            log.Append(EventType.PlayerEliminated, previousOwner.Id, $"{previousOwner.Name} has been eliminated.");
        }

        if (player.OwnedCountryIds.Count == game.Territories.Count)
        {
            AttackSteps.End(game, log, player, "conquest.");
            game.Phase = GamePhase.Finished;
            game.WinnerPlayerId = player.Id;
            log.Append(
                EventType.GameWon,
                player.Id,
                $"{player.Name} controls all {game.Territories.Count} countries and wins the game.");
            return;
        }

        // The attack stays open until the player chooses how many soldiers to move in.
        game.Phase = GamePhase.ConquestTransfer;
    }
}