using Risk.Api.DataStructures;
using Risk.Api.DesignPatterns.Command.Interfaces;
using Risk.Api.DesignPatterns.Strategy.Interfaces;
using Risk.Api.Engine;
using Risk.Api.Models;

namespace Risk.Api.DesignPatterns.Command;

// Plays one full turn for the NPC whose turn it is: reinforce, attack (or skip), then end the turn.
public sealed class NpcTurnCommand : IGameCommand
{
    private const int MaxRollsPerAttack = 500;

    private readonly INpcStrategy _strategy;

    public NpcTurnCommand(INpcStrategy strategy) => _strategy = strategy;

    public CommandResult Execute(Game game, EventLog log)
    {
        if (game.Phase == GamePhase.Finished)
        {
            return CommandResult.Fail("The game is over.");
        }

        var npc = TurnRules.FindPlayer(game, game.TurnQueue.Peek());
        if (npc is null || npc.IsHuman)
        {
            return CommandResult.Fail("It is your turn.");
        }

        var error = Play(game, log, npc);
        if (error is not null) 
        {
            return CommandResult.Fail(error);
        }

        EndTheGameIfTheHumanIsOut(game, log);
        return CommandResult.Ok();
    }

    private string? Play(Game game, EventLog log, Player npc)
    {
        // 1. Reinforcements (from round 2).
        if (game.Phase == GamePhase.Reinforcing)
        {
            foreach (var placement in _strategy.PlanReinforcements(game, npc))
            {
                Decide(log, npc,
                    $"{npc.Name} reinforces {NameOf(game, placement.CountryId)} with {placement.Soldiers} soldiers.",
                    placement.Justification);

                var placed = new PlaceReinforcementsCommand(npc.Id, placement.CountryId, placement.Soldiers).Execute(game, log);
                if (!placed.Success) return placed.Error;
            }
        }

        // 2. Attack, or skip the attack.
        var attack = _strategy.ChooseAttack(game, npc);
        if (attack is null)
        {
            Decide(log, npc, $"{npc.Name} does not attack this turn.", "No target offers good enough odds.");
            return FinishTurn(game, log, npc);
        }

        Decide(log, npc,
            $"{npc.Name} attacks {NameOf(game, attack.ToCountryId)} from {NameOf(game, attack.FromCountryId)}.",
            attack.Justification);

        var started = new StartAttackCommand(npc.Id, attack.FromCountryId, attack.ToCountryId).Execute(game, log);
        if (!started.Success) return started.Error;

        for (var rolls = 0; rolls < MaxRollsPerAttack; rolls++)
        {
            var rolled = new RollAttackCommand(npc.Id).Execute(game, log);
            if (!rolled.Success) return rolled.Error;

            if (game.Phase == GamePhase.Finished) return null;

            if (game.Phase == GamePhase.ConquestTransfer)
            {
                var move = _strategy.ChooseConquestTransfer(game, npc);
                Decide(log, npc,
                    $"{npc.Name} moves {move.Soldiers} soldiers into {NameOf(game, move.ToCountryId)}.",
                    move.Justification);

                var moved = new TransferAfterConquestCommand(npc.Id, move.Soldiers).Execute(game, log);
                if (!moved.Success) return moved.Error;

                return FinishTurn(game, log, npc);
            }

            // The attacker ran out of soldiers: the attack has already ended.
            if (game.Phase == GamePhase.Action) return FinishTurn(game, log, npc);

            if (game.Phase != GamePhase.Attacking) return "The attack ended in an unexpected state.";

            var decision = _strategy.DecideContinue(game, npc);
            if (decision.Continue)
            {
                Decide(log, npc, $"{npc.Name} keeps rolling.", decision.Justification);
                continue;
            }

            Decide(log, npc, $"{npc.Name} stops the attack.", decision.Justification);

            // Stopping an attack also ends the turn.
            return new StopAttackCommand(npc.Id).Execute(game, log).Error;
        }

        return "The attack never ended.";
    }

    // An optional move between the NPC's own countries, then the end of the turn.
    private string? FinishTurn(Game game, EventLog log, Player npc)
    {
        var transfer = _strategy.ChooseTransfer(game, npc);
        if (transfer is not null)
        {
            Decide(log, npc,
                $"{npc.Name} moves {transfer.Soldiers} soldiers from {NameOf(game, transfer.FromCountryId)} to {NameOf(game, transfer.ToCountryId)}.",
                transfer.Justification);

            var moved = new TransferCommand(npc.Id, transfer.FromCountryId, transfer.ToCountryId, transfer.Soldiers).Execute(game, log);
            if (!moved.Success) return moved.Error;
        }

        return new EndTurnCommand(npc.Id).Execute(game, log).Error;
    }

    // Once the human has no countries left the game is lost, so there is no point simulating further.
    private static void EndTheGameIfTheHumanIsOut(Game game, EventLog log)
    {
        var human = game.Players.First(p => p.IsHuman);
        if (!human.IsEliminated || game.Phase == GamePhase.Finished) return;

        game.Phase = GamePhase.Finished;
        game.ActiveAttack = null;
        log.Append(EventType.GameLost, human.Id, $"{human.Name} has lost every country. The game is over.");
    }

    private static void Decide(EventLog log, Player npc, string message, string justification) =>
        log.Append(EventType.NpcDecision, npc.Id, message, justification);

    private static string NameOf(Game game, string countryId) => game.Territories[countryId].Name;
}