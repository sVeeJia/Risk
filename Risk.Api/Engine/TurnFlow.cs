using Risk.Api.DataStructures;
using Risk.Api.Models;

namespace Risk.Api.Engine;

public static class TurnFlow
{
    public const int ReinforcementsPerCountry = 2;

    // Ends the current player's turn and starts the next active player's turn.
    public static void Advance(Game game, EventLog log)
    {
        var finished = game.TurnQueue.Dequeue();
        game.TurnQueue.Enqueue(finished);

        // Games saved before this field existed: the player ending now becomes the round starter.
        game.RoundStartPlayerId ??= finished;

        for (var checkedPlayers = 0; checkedPlayers <= game.Players.Count; checkedPlayers++)
        {
            var nextId = game.TurnQueue.Peek();

            // Play has come back around to the round starter: a new round begins.
            // This also counts when the round starter has been eliminated and is skipped.
            if (nextId == game.RoundStartPlayerId) game.Round++;

            var next = TurnRules.FindPlayer(game, nextId)!;
            if (!next.IsEliminated)
            {
                StartTurn(game, log, next);
                return;
            }

            log.Append(EventType.TurnSkipped, next.Id, $"{next.Name} is eliminated and skips their turn.");
            game.TurnQueue.Enqueue(game.TurnQueue.Dequeue());
        }

        throw new InvalidOperationException("No active player is left to take a turn.");
    }

    private static void StartTurn(Game game, EventLog log, Player player)
    {
        game.AttackedThisTurn = false;
        game.ActiveAttack = null;

        log.Append(
            EventType.TurnStarted,
            player.Id,
            $"{player.Name}'s turn begins (round {game.Round}).",
            data: new Dictionary<string, string> { ["round"] = game.Round.ToString() });

        // Round 1 has no reinforcements.
        if (game.Round < 2)
        {
            player.PendingReinforcements = 0;
            game.Phase = GamePhase.Action;
            return;
        }

        var reinforcements = ReinforcementsPerCountry * player.OwnedCountryIds.Count;
        player.PendingReinforcements = reinforcements;
        game.Phase = GamePhase.Reinforcing;

        log.Append(
            EventType.ReinforcementsReceived,
            player.Id,
            $"{player.Name} receives {reinforcements} reinforcements.",
            data: new Dictionary<string, string> { ["soldiers"] = reinforcements.ToString() });
    }
}